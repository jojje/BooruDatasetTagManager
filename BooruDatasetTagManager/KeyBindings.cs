using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Linq;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using static BooruDatasetTagManager.KeyCodeConverter;

namespace BooruDatasetTagManager
{
    /// <summary>
    /// Set of all key-bindings in the application.
    /// </summary>
    ///
    /// ## Adding _new_ shortcuts
    /// To add a new keyboard shortcut (key-bound command), three types of changes are needed:
    /// 1. Declare a name for the command in the Commands class.
    /// 2. Add the key binding for that name to the CommandKeyMap dictionary (see existing ones).
    /// 3. Add a new ICommand implementation with the same `Name` as declared in the previous step.
    ///
    /// ## Overview
    /// The idea with the key-binding architecture is that it should be minimally invasive to existing code.  It uses
    /// the Command pattern and works by having a Form register with the KeyBinder, which adds (another) key-event
    /// listener to the form; a listener which is the keyBinder. The binder reacts to pressed keys that have commands
    /// registered, and invokes the corresponding commands based on a keycode-to-command mapping (found in the
    /// `CommandKeyMap`).
    ///
    /// ## Data flow
    /// 1. Some key-event happens on a registered form.
    /// 2. KeyBinder listener gets called with the key-event.
    /// 3. KeyBinder checks if the keyCode has a corresponding command ("shortcut") binding registered.
    /// 4. If there is a command registered for the key-code, then that command's Execute method is invoked.
    ///
    /// ## User configurability
    /// The key-binding configuration gets saved with the rest of the application configuration into the application's
    /// settings (json) file. When the app starts up, the config is deserialized and the key-bindings restored.
    ///
    /// If the user changes a key-binding in the Shortcuts settings menu, that change gets propagated to the
    /// CommandKeyMap. Conseuqnely when the user saves the settings, any changes in the key-bindings get saved as well.
    ///
    [JsonConverter(typeof(KeyBindingJsonConverter))]
    public class KeyBindings
    {
        public Dictionary<string, Keys> CommandKeyMap { get; set; }  // needs to be public to make it json serializable :(

        public KeyBindings() => CommandKeyMap = new Dictionary<string, Keys>
        {
            { Commands.AddTag, Keys.Control | Keys.A },
            { Commands.ApplyTags, Keys.Control | Keys.Enter },
            { Commands.DeleteTag, Keys.Control | Keys.X},
            { Commands.EditSelectedTag, Keys.Control | Keys.E},
        };

        /// <summary> Assign a new key-binding to an existing command </summary>
        public void Update(string commandName, Keys keyCode)
        {
            if (!CommandKeyMap.ContainsKey(commandName)) return;
            CommandKeyMap[commandName] = keyCode;
        }
    }

    /// <summary>Binds the key-bindings to corresponding actions</summary>
    [Serializable]
    public class KeyBinder
    {
        private readonly KeyBindings keyBindings;
        private readonly Dictionary<Tuple<Keys, Keys>, ICommand> keyCommandMap = new Dictionary<Tuple<Keys, Keys>, ICommand>();

        public KeyBinder(KeyBindings bindings)
        {
            this.keyBindings = bindings;
        }

        public void RegisterCommand(ICommand command)
        {
            if (keyBindings.CommandKeyMap.TryGetValue(command.Name, out Keys compositeKey))
            {
                var key = compositeKey & Keys.KeyCode;
                var modifiers = compositeKey & Keys.Modifiers;
                keyCommandMap[new Tuple<Keys, Keys>(key, modifiers)] = command;
            }
        }

        public void BindKeyEvents(Form form)
        {
            form.KeyPreview = true;
            form.KeyDown += Form_KeyDown;

            if (form is MainForm mainForm)
            {
                Program.KeyBinder.RegisterCommand(new Commands.AddTagCommand(mainForm));
                Program.KeyBinder.RegisterCommand(new Commands.ApplyTagsCommand(mainForm));
                Program.KeyBinder.RegisterCommand(new Commands.DeleteTagCommand(mainForm));
                Program.KeyBinder.RegisterCommand(new Commands.EditSelectedTagCommand(mainForm));
            }
        }

        private void Form_KeyDown(object sender, KeyEventArgs e)
        {
            var keyTuple = new Tuple<Keys, Keys>(e.KeyCode, e.Modifiers);

            if (keyCommandMap.TryGetValue(keyTuple, out ICommand command))
            {
                command.Execute();
            }
        }

        public ICommand FindCommandByName(string name)  // inefficient, but it's a small data set and seldom used
        {
            var found = keyCommandMap.FirstOrDefault(kv => kv.Value.Name == name);
            if (found.Key == null) return null;
            return found.Value;
        }
    }

    public interface ICommand
    {
        string Name { get; }
        string Description { get; }
        void Execute();
    }

    // =====================================================================================
    // Command implementations
    // =====================================================================================

    /// <summary> collection of all command names in the program </summary>
    public static class Commands
    {
        public const string AddTag = "AddTag";
        public const string ApplyTags = "ApplyTags";
        public const string DeleteTag = "DeleteTag";
        public const string EditSelectedTag = "EditSelectedTag";

        public class AddTagCommand : ICommand
        {
            public string Name => Commands.AddTag;
            public string Description => "Add a new tag to the image";
            private readonly MainForm form;

            public AddTagCommand(MainForm form)
            {
                this.form = form; // needs a reference to the form which implements the action to execute
            }

            public void Execute()
            {
                form.AddNewRow();
            }
        }

        public class ApplyTagsCommand : ICommand
        {
            public string Name => Commands.ApplyTags;
            public string Description => "Apply created or edited tags to the image";
            private readonly MainForm form;

            public ApplyTagsCommand(MainForm form)
            {
                this.form = form;
            }

            public void Execute()
            {
                form.ApplyTagsChanges();
            }
        }

        public class DeleteTagCommand : ICommand
        {
            public string Name => Commands.DeleteTag;
            public string Description => "Delete the selected tag";
            private readonly MainForm form;

            public DeleteTagCommand(MainForm form)
            {
                this.form = form;
            }

            public void Execute()
            {
                form.DeleteTag();
            }
        }

        public class EditSelectedTagCommand : ICommand
        {
            public string Name => Commands.EditSelectedTag;
            public string Description => "Edits the selected tag";
            private readonly MainForm form;

            public EditSelectedTagCommand(MainForm form)
            {
                this.form = form;
            }

            public void Execute()
            {
                if (form.gridViewTags.CurrentCell == null) return;
                form.gridViewTags.BeginEdit(true);
            }
        }
    }

}
