﻿using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Graphics;
using Helion.Layer.Options.Sections;
using Helion.Render.Common.Enums;
using Helion.Render.Common.Renderers;
using Helion.Util;
using Helion.Util.Configs.Components;
using Helion.Util.Configs.Extensions;
using Helion.Util.Configs.Options;
using Helion.Util.Configs.Values;
using Helion.Util.Timing;
using Helion.Window;
using Helion.Window.Input;
using System;

namespace Helion.Layer.Options;

internal class ColorDialog : IDialog
{
    const string Font = Constants.Fonts.SmallGray;
    const string Selector = ">";

    public event EventHandler<DialogCloseArgs>? OnClose;

    private readonly ConfigHud m_config;
    private readonly IConfigValue m_configValue;
    private readonly OptionMenuAttribute m_attr;
    private readonly Slider m_redSlider;
    private readonly Slider m_greenSlider;
    private readonly Slider m_blueSlider;
    private readonly Slider[] m_sliders;
    private int m_rowHeight;
    private int m_padding;
    private int m_row;
    private int m_valueStartX;
    private int m_fontSize;
    private Dimension m_box;
    private Dimension m_selectorSize;
    private Vec3I m_color;

    public Vec3I SelectedColor => m_color;
    public IConfigValue ConfigValue => m_configValue;

    public ColorDialog(ConfigHud config, IConfigValue configValue, OptionMenuAttribute attr, Vec3I color)
    {
        m_config = config;
        m_configValue = configValue;
        m_attr = attr;
        m_color = color;
        m_redSlider = CreateSlider(color.X);
        m_greenSlider = CreateSlider(color.Y);
        m_blueSlider = CreateSlider(color.Z);
        m_sliders = [m_redSlider, m_greenSlider, m_blueSlider];
        m_redSlider.ValueChanged += (s, e) => { m_color.X = (int)e; };
        m_greenSlider.ValueChanged += (s, e) => { m_color.Y = (int)e; };
        m_blueSlider.ValueChanged += (s, e) => { m_color.Z = (int)e; };
    }

    private static Slider CreateSlider(int value)
    {
        var slider = new Slider(value, 8, 0, 255);
        slider.MaxOffset = 1;
        return slider;
    }

    public void Dispose()
    {
    }

    public void HandleInput(IConsumableInput input)
    {
        if (input.ConsumeKeyPressed(Key.Escape))
            OnClose?.Invoke(this, new(false));
        if (input.ConsumeKeyPressed(Key.Enter))
            OnClose?.Invoke(this, new(true));

        if (input.ConsumePressOrContinuousHold(Key.Down))
            m_row = ++m_row;
        if (input.ConsumePressOrContinuousHold(Key.Up))
            m_row = --m_row;

        if (m_row < 0)
            m_row = 2;
        if (m_row > 2)
            m_row = 0;

        m_sliders[m_row].HandleInput(input);
    }

    public void Render(IRenderableSurfaceContext ctx, IHudRenderContext hud)
    {
        m_fontSize = m_config.GetSmallFontSize();
        m_padding = m_config.GetScaled(8);
        int border = m_config.GetScaled(1);
        var size = new Dimension(Math.Max(hud.Width / 2, 320), Math.Max(hud.Height / 2, 200));
        hud.FillBox((0, 0, hud.Width, hud.Height), Color.Black, alpha: 0.5f);

        m_box = new(size.Width - m_padding * 2, size.Height - m_padding * 2);

        hud.FillBox((0, 0, size.Width, size.Height), Color.Gray, window: Align.Center, anchor: Align.Center);
        hud.FillBox((0, 0, size.Width - (border * 2), size.Height - (border * 2)), Color.Black, window: Align.Center, anchor: Align.Center);

        m_selectorSize = hud.MeasureText(Selector, Font, m_fontSize);
        m_rowHeight = hud.MeasureText("I", Font, m_fontSize).Height;
        m_valueStartX = hud.MeasureText("Green", Font, m_fontSize).Width + m_padding * 4;

        hud.PushOffset((0, size.Height / 2 + m_padding));

        hud.Text(m_attr.Name, Font, m_fontSize, (0, 0), window: Align.TopMiddle, anchor: Align.TopMiddle);
        hud.AddOffset((0, m_rowHeight + m_padding));
        int boxSize = m_config.GetScaled(24);
        RenderColorBox(hud, 0, 0, boxSize);

        hud.AddOffset((size.Width / 2 + m_padding, boxSize + m_rowHeight));
        hud.Text(Selector, Font, m_fontSize, (0, m_row * (m_rowHeight + m_padding)));
        RenderSlider(ctx, hud, "Red", m_redSlider);
        RenderSlider(ctx, hud, "Green", m_greenSlider);
        RenderSlider(ctx, hud, "Blue", m_blueSlider);

        hud.PopOffset();
    }

    private void RenderSlider(IRenderableSurfaceContext ctx, IHudRenderContext hud, string text, Slider slider)
    {
        text = ListedConfigSection.GetEllipsesText(hud, text, Font, m_fontSize, m_box.Width);
        hud.Text(text, Font, m_fontSize, (m_selectorSize.Width + m_padding, 0), color: Color.Red);
        int numWidth = hud.MeasureText("999", Font, m_fontSize).Width;
        hud.AddOffset((m_valueStartX, 0), () =>
        {
            slider.Width = new(Math.Clamp(m_box.Width - m_valueStartX - numWidth - m_padding, 0, 320), SizeMetric.Pixel);
            slider.Render(m_config, ctx, hud);
        });
        hud.AddOffset((0, m_rowHeight + m_padding));
    }

    private void RenderColorBox(IHudRenderContext hud, int x, int y, int boxSize)
    {
        var boxColor = new Color(m_color);
        hud.FillBox((x, y, x + boxSize, y + boxSize), Color.White, window: Align.TopMiddle,
            anchor: Align.TopMiddle);
        hud.FillBox((x, y + 1, x + boxSize - 2, y + boxSize - 1), boxColor, window: Align.TopMiddle,
            anchor: Align.TopMiddle);
    }

    public void RunLogic(TickerInfo tickerInfo)
    {
    }
}
