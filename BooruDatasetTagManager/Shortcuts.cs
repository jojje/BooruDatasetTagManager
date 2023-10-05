using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Linq;
using Newtonsoft.Json;
using System.Runtime.InteropServices;

namespace BooruDatasetTagManager
{
    /// <summary>
    /// Set of all key-bindings in the application.
    /// </summary>
    ///
    /// ## Adding _new_ shortcuts
    /// To add a new keyboard shortcut (key-bound command), two types of changes are needed:
    /// 1. Add a new ICommand implementation.
    /// 2. Register (declare) the command in the Shortcuts constructor. See existing ones as examples.
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
    public sealed class Shortcuts
    {
        public static readonly Shortcuts Instance = new Shortcuts();
        private readonly Dictionary<string, (Type, Keys)> commands;
        private readonly Dictionary<Keys, string> nameByKey = new Dictionary<Keys, string>();
        private readonly Dictionary<string, ICommand> commandByName = new Dictionary<string, ICommand>();

        private Shortcuts()
        {
            // format: (name of command, command class, default key-binding)
            commands = new Dictionary<string, (Type, Keys)> {
                {"AddTag", (typeof(AddTagCommand), Keys.Control | Keys.A ) },
                {"ApplyTags", (typeof(ApplyTagsCommand), Keys.Control | Keys.Enter ) },
                {"DeleteTag", (typeof(DeleteTagCommand), Keys.Control | Keys.Shift | Keys.X ) },
                {"EditSelectedTag", (typeof(EditSelectedTagCommand), Keys.Control | Keys.E ) },
                {"FocusImageList", (typeof(FocusImageListCommand), Keys.Escape ) },
                {"FocusTagList", (typeof(FocusTagListCommand), Keys.Control | Keys.T) },
            };
            KeyBindings = commands.ToDictionary(t => t.Key, t => t.Value.Item2);  // setup the key -> name mapping
            InterceptKeys.AddKeyEventListener(Form_KeyDown);
        }

        /// <summary>
        /// Current key-bindings of the app, represented as {command-name: key-shortcut}
        /// </summary>
        public Dictionary<string, Keys> KeyBindings
        {
            get => commands.ToDictionary(kv => kv.Key, kv => kv.Value.Item2);
            set
            {
                foreach (var (name, key) in value)
                {
                    if (commands.ContainsKey(name))
                    {
                        var cls = commands[name].Item1;
                        commands[name] = (cls, key);
                        nameByKey[key] = name;
                    }
                }
            }
        }

        /// <summary>
        /// Makes variables in the main form available to commands.
        /// </summary>
        /// <param name="mainForm"></param>
        public void Register(MainForm mainForm)
        {
            foreach (var (name, (cls, _)) in commands)
            {
                var command = (ICommand)Activator.CreateInstance(cls, mainForm);
                commandByName[name] = command;
            }
        }

        public void UpdateShortcut(string commandName, Keys key)
        {
            var copy = KeyBindings;
            copy[commandName] = key;
            KeyBindings = copy;
        }

        private void Form_KeyDown(object sender, KeyEventArgs e)
        {
            if (!isMainFormInForeground()) return;  // only run commands from the main window so as not to trigger commands during user keybinding reconfiguration in settings.
            if (nameByKey.TryGetValue(e.KeyCode | e.Modifiers, out string name))
            {
                var command = commandByName[name];  // guaranteed to always exist after form registration, if present in nameByKey
                e.Handled = command.Execute();
            }
        }

        public ICommand FindCommandByName(string name)
        {
            var found = commandByName.TryGetValue(name, out var command);
            if (!found) throw new NullReferenceException("No such command named: " + name);
            return command;
        }

        private bool isMainFormInForeground()
        {
            IntPtr handle = GetForegroundWindow();
            if (!(Form.FromHandle(handle) is Form foregroundForm)) return false;
            return foregroundForm.GetType() == typeof(MainForm);
        }

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();
    }

    public interface ICommand
    {
        string Description { get; }
        bool Execute();
    }

    // =====================================================================================
    // Command implementations
    // =====================================================================================

    /// <summary> collection of all command names in the program </summary>

    public class AddTagCommand : ICommand
    {
        public string Description => "Add a new tag to the image";
        private readonly MainForm form;

        public AddTagCommand(MainForm form) { this.form = form; }

        public bool Execute()
        {
            form.AddNewRow();
            return true;
        }
    }

    public class ApplyTagsCommand : ICommand
    {
        public string Description => "Apply created or edited tags to the image";
        private readonly MainForm form;

        public ApplyTagsCommand(MainForm form) { this.form = form; }

        public bool Execute()
        {
            form.ApplyTagsChanges();
            return true;
        }
    }

    public class DeleteTagCommand : ICommand
    {
        public string Description => "Delete the selected tag";
        private readonly MainForm form;

        public DeleteTagCommand(MainForm form) { this.form = form; }

        public bool Execute()
        {
            form.DeleteTag();
            return true;
        }
    }

    public class EditSelectedTagCommand : ICommand
    {
        public string Description => "Edits the selected tag";
        private readonly MainForm form;

        public EditSelectedTagCommand(MainForm form) { this.form = form; }

        public bool Execute()
        {
            if (form.gridViewTags.CurrentCell == null) return false;
            form.gridViewTags.BeginEdit(true);
            return true;
        }
    }

    public class FocusImageListCommand : ICommand
    {
        public string Description => "Set focus on image list";
        private readonly MainForm form;

        public FocusImageListCommand(MainForm form) { this.form = form; }

        public bool Execute()
        {
            if (form.gridViewTags.IsCurrentCellInEditMode) return false;  // Let the control exit edit mode using standard ESC shortcut.
            form.gridViewDS.Focus();
            return true;
        }
    }

    public class FocusTagListCommand : ICommand
    {
        public string Description => "Set focus on tag list";
        private readonly MainForm form;

        public FocusTagListCommand(MainForm form) { this.form = form; }

        public bool Execute()
        {
            if (form.gridViewTags.IsCurrentCellInEditMode) return false;
            form.gridViewTags.Focus();
            return true;
        }
    }
}