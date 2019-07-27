using System.Collections.Generic;
using System.Linq;
using Helion.Util;

namespace Helion.Resources.Definitions.Decorate
{
    public class DecorateDefinitions
    {
        private readonly Dictionary<CIString, ActorDefinition> m_definitions = new Dictionary<CIString, ActorDefinition>();
        private readonly Dictionary<int, ActorDefinition> m_definitionsByEditorNumber = new Dictionary<int, ActorDefinition>();

        public DecorateDefinitions()
        {
            AddTemporaryPlayerDefinition();
        }

        public ActorDefinition? this[CIString name] => m_definitions.TryGetValue(name, out ActorDefinition def) ? def : null;
        public ActorDefinition? this[int editorNum] => m_definitionsByEditorNumber.TryGetValue(editorNum, out ActorDefinition def) ? def : null;

        public bool Contains(CIString name) => m_definitions.ContainsKey(name);
        
        private void AddDefinition(ActorDefinition definition)
        {
            m_definitions[definition.Name] = definition;
            
            if (definition.EditorNumber != null)
                m_definitionsByEditorNumber[definition.EditorNumber.Value] = definition;
        }
        
        private void AddTemporaryPlayerDefinition()
        {
            // This is something we're doing just so we can run around in the
            // map until we can parse decorate files.
            ActorDefinition player = new ActorDefinition(Constants.PlayerClass);
            player.Flags.Solid = true;
            player.Flags.SlidesOnWalls = true;
            player.Properties.Height = 56.0;
            player.Properties.Radius = 16.0;
            player.Properties.StepHeight = 24.0;

            AddDefinition(player);
        }
    }
}