﻿using System;
using System.Diagnostics;
using System.Drawing;
using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Graphics.String;
using Helion.Menus;
using Helion.Menus.Base;
using Helion.Menus.Base.Text;
using Helion.Render.OpenGL.Legacy.Commands;
using Helion.Render.OpenGL.Legacy.Commands.Alignment;
using Helion.Render.OpenGL.Legacy.Shared.Drawers.Helper;
using Helion.Resources.Archives.Collection;
using Helion.Util;
using Font = Helion.Graphics.Fonts.Font;

namespace Helion.Render.OpenGL.Legacy.Shared.Drawers
{
    public class MenuDrawer
    {
        private const int ActiveMillis = 500;

        private const int SelectedOffsetX = -32;
        private const int SelectedOffsetY = 5;

        private readonly ArchiveCollection m_archiveCollection;
        private readonly Stopwatch m_stopwatch = new();

        public MenuDrawer(ArchiveCollection archiveCollection)
        {
            m_archiveCollection = archiveCollection;
            m_stopwatch.Start();
        }

        public void Draw(Menu menu, RenderCommands renderCommands)
        {
            DrawHelper helper = new(renderCommands);
            
            helper.AtResolution(DoomHudHelper.DoomResolutionInfoCenter, () =>
            {
                int offsetY = menu.TopPixelPadding;
                
                foreach (IMenuComponent component in menu)
                {
                    bool isSelected = ReferenceEquals(menu.CurrentComponent, component);
                        
                    switch (component)
                    {
                    case MenuImageComponent imageComponent:
                        DrawImage(helper, imageComponent, isSelected, ref offsetY);
                        break;
                    case MenuPaddingComponent paddingComponent:
                        offsetY += paddingComponent.PixelAmount;
                        break;
                    case MenuSmallTextComponent smallTextComponent:
                        DrawText(helper, smallTextComponent, ref offsetY);
                        break;
                    case MenuLargeTextComponent largeTextComponent:
                        DrawText(helper, largeTextComponent, ref offsetY);
                        break;
                    case MenuSaveRowComponent saveRowComponent:
                        DrawSaveRow(helper, saveRowComponent, isSelected, ref offsetY);
                        break;
                    default:
                        throw new Exception($"Unexpected menu component type for drawing: {component.GetType().FullName}");
                    }
                }
            });
        }

        private void DrawText(DrawHelper helper, MenuTextComponent text, ref int offsetY)
        {
            Font? font = m_archiveCollection.GetFont(text.FontName);
            if (font == null)
                return;
            
            helper.Text(text.Text, font, text.Size, out Dimension area, 0, offsetY, both: Align.TopMiddle);
            offsetY += area.Height;
        }

        private void DrawImage(DrawHelper helper, MenuImageComponent image, bool isSelected, ref int offsetY)
        {
            int drawY = image.PaddingTopY + offsetY;
            if (image.AddToOffsetY)
                offsetY += image.PaddingTopY;

            var dimension = helper.DrawInfoProvider.GetImageDimension(image.ImageName);
            Vec2I offset = helper.DrawInfoProvider.GetImageOffset(image.ImageName);
            helper.TranslateDoomOffset(ref offset, dimension);
            int offsetX = image.OffsetX + offset.X;

            helper.Image(image.ImageName, offsetX, drawY + offset.Y, out Dimension area, both: image.ImageAlign);

            if (isSelected)
            {
                string selectedName = (ShouldDrawActive() ? image.ActiveImage : image.InactiveImage) ?? string.Empty;
                dimension = helper.DrawInfoProvider.GetImageDimension(selectedName);
                offsetX += SelectedOffsetX;
                Vec2I selectedOffset = helper.DrawInfoProvider.GetImageOffset(selectedName);
                helper.TranslateDoomOffset(ref selectedOffset, dimension);

                helper.Image(selectedName, offsetX + selectedOffset.X,
                    drawY + selectedOffset.Y - SelectedOffsetY, both: image.ImageAlign);
            }
            
            if (image.AddToOffsetY)
                offsetY += area.Height + offset.Y + image.PaddingBottomY;
        }

        private bool ShouldDrawActive() => (m_stopwatch.ElapsedMilliseconds % ActiveMillis) <= ActiveMillis / 2;
        
        private void DrawSaveRow(DrawHelper helper, MenuSaveRowComponent saveRowComponent, bool isSelected, 
            ref int offsetY)
        {
            const int LeftOffset = 64;
            const int RowVerticalPadding = 8;
            const int SelectionOffsetX = 4;
            
            Font? font = m_archiveCollection.GetFont("SmallFont");
            if (font == null)
                return;
            
            if (isSelected)
            {
                string selectedName = ShouldDrawActive() ? Constants.MenuSelectIconActive : Constants.MenuSelectIconInactive;
                var (w, _) = helper.DrawInfoProvider.GetImageDimension(selectedName);
                
                helper.Image(selectedName, LeftOffset - w - SelectionOffsetX, offsetY);
            }

            var (leftWidth, leftHeight) = helper.DrawInfoProvider.GetImageDimension("M_LSLEFT");
            var (middleWidth, middleHeight) = helper.DrawInfoProvider.GetImageDimension("M_LSCNTR");
            var (rightWidth, rightHeight) = helper.DrawInfoProvider.GetImageDimension("M_LSRGHT");
            int offsetX = LeftOffset;

            helper.Image("M_LSLEFT", offsetX, offsetY);
            offsetX += leftWidth;

            int blocks = (int)Math.Ceiling((MenuSaveRowComponent.PixelWidth - leftWidth - rightWidth) / (double)middleWidth); 
            for (int i = 0; i < blocks; i++)
            {
                helper.Image("M_LSCNTR", offsetX, offsetY);
                offsetX += middleWidth;
            }
            
            helper.Image("M_LSRGHT", offsetX, offsetY);

            ColoredString text = ColoredStringBuilder.From(Color.Red, saveRowComponent.Text);
            helper.Text(text, font, 8, out Dimension area, LeftOffset + leftWidth + 4, offsetY + 3);

            offsetY += MathHelper.Max(area.Height, leftHeight, middleHeight, rightHeight) + RowVerticalPadding;
        }
    }
}
