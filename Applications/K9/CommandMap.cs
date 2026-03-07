// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.


using System.Text;


namespace K9
{
    public class CommandMap
    {
        readonly Dictionary<string, CommandMapAction> _map = [];

        public bool HasCommands()
        {
            return _map.Count > 0;
        }

        public class CommandMapAction
        {
            public string? Command { get; set; }
            public string? Description { get; set; }
            public string? WorkingDirectory { get; set; }
            public string? Arguments { get; set; }

            public Dictionary<string, CommandMapAction> Children { get; set; } = [];

            public CommandMapAction(Commands.CommandVerb action)
            {
                Command = action.Command;
                Description = action.Description;
                WorkingDirectory = action.WorkingDirectory;
                Arguments = action.Arguments;

                if (action.Actions is not { Length: > 0 })
                {
                    return;
                }

                int count = action.Actions.Length;

                for(int i = 0; i < count; i++)
                {
                    Commands.CommandVerb childAction = action.Actions[i];
                    if (childAction.Identifier == null)
                    {
                        continue;
                    }

                    if (!Children.TryGetValue(childAction.Identifier, out CommandMapAction? value))
                    {
                        Children.Add(childAction.Identifier, new CommandMapAction(childAction));
                    }
                    else
                    {
                        value.Append(childAction);
                    }
                }
            }
            public void Append(Commands.CommandVerb action)
            {
                Command = action.Command;
                Description = action.Description;
                WorkingDirectory = action.WorkingDirectory;
                Arguments = action.Arguments;

                if (action.Actions is not { Length: > 0 })
                {
                    return;
                }

                int count = action.Actions.Length;

                for (int i = 0; i < count; i++)
                {
                    Commands.CommandVerb childAction = action.Actions[i];
                    if (childAction.Identifier == null)
                    {
                        continue;
                    }

                    if (!Children.TryGetValue(childAction.Identifier, out CommandMapAction? value))
                    {
                        Children.Add(childAction.Identifier, new CommandMapAction(childAction));
                    }
                    else
                    {
                        value.Append(childAction);
                    }
                }
            }

            public void AddHelpCommands(List<HelpCommand> commands, string prefix = "")
            {
                if (!string.IsNullOrEmpty(Command))
                {
                    commands.Add(new HelpCommand(prefix, Description));
                }

                if(Children.Count > 0)
                {
                    foreach(KeyValuePair<string, CommandMapAction> kvp in Children)
                    {
                        kvp.Value.AddHelpCommands(commands, $"{prefix} {kvp.Key}");
                    }
                }
            }
        }

        public void AddCommands(Commands commands)
        {
            int count = commands.Actions.Length;
            for (int i = 0; i < count; i++)
            {
                Commands.CommandVerb action = commands.Actions[i];
                if(action.Identifier != null)
                {
                    if (!_map.TryGetValue(action.Identifier, out CommandMapAction? value))
                    {
                        _map.Add(action.Identifier, new CommandMapAction(action));
                    }
                    else
                    {
                        value.Append(action);
                    }
                }
            }
        }

        public CommandMapAction? GetAction(string query)
        {
            string[] parts = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            int partCount = parts.Length;
            int depth = 0;

            // Early out
            if (partCount == 1)
            {
                return _map.GetValueOrDefault(parts[0]);
            }

            // Find the right base
            CommandMapAction? currentActionMap = null;
            if (_map.TryGetValue(parts[0], out CommandMapAction? value))
            {
                currentActionMap = value;
            }
            if (currentActionMap == null) return null;
            depth++;

            while (depth < partCount)
            {
                if (currentActionMap.Children.TryGetValue(parts[depth], out CommandMapAction? found))
                {
                    currentActionMap = found;
                    depth++;

                    if (depth == partCount)
                    {
                        return currentActionMap;
                    }
                    continue;
                }
                return null;
            }
            return null;
        }

        public string GetOutput()
        {
            List<HelpCommand> commands = [];
            foreach(KeyValuePair<string, CommandMapAction> kvp in _map)
            {
                // Odd case where a top level command exists
                if (kvp.Value.Command != null)
                {
                    commands.Add(new HelpCommand(kvp.Key, kvp.Value.Description));

                }

                if(kvp.Value.Children.Count > 0)
                {
                    kvp.Value.AddHelpCommands(commands, kvp.Key);
                }
            }

            StringBuilder builder = new();

            builder.AppendLine("K9 Registered Actions");
            builder.AppendLine();

            int helpCount = commands.Count;
            int lhsCharacterCount = 0;
            for(int i = 0; i < helpCount; i++)
            {
                if(commands[i].Command.Length > lhsCharacterCount)
                {
                    lhsCharacterCount = commands[i].Command.Length;
                }
            }

            int padding = lhsCharacterCount + 5;
            for(int i = 0; i < helpCount; i++)
            {
                builder.AppendLine($"{commands[i].Command.PadRight(padding)}{commands[i].Description}");
            }


            return builder.ToString();
        }

        public struct HelpCommand(string command, string? description)
        {
            public string Command { get; set; } = command;
            public string? Description { get; set; } = description;
        }
    }
}
