namespace Markdown.Renderer.MarkdigExtension;

/// <summary>
/// A key that <c>++…++</c> can name. <see cref="Symbol"/> is the sign printed on real keyboards, such as ⌘ or ⇧;
/// keys with <see cref="SymbolOnly"/> show only the symbol, like the arrow keys on a keyboard.
/// </summary>
public sealed record KeyboardKey(string Name, string Label, string? Symbol = null, bool SymbolOnly = false);

/// <summary>
/// The key names and aliases of the pymdownx Keys extension used by Material for MkDocs (English US key map),
/// plus <c>globe</c> for the 🌐 key of Apple keyboards.
/// </summary>
public static class KeyboardKeys
{
    // Variation selector 15 asks for the plain symbol instead of a colored emoji.
    private const string TextStyle = "\uFE0E";
    private static readonly Dictionary<string, KeyboardKey> KeysByName = Create();

    public static bool TryGetKey(string nameOrAlias, out KeyboardKey key) =>
        KeysByName.TryGetValue(nameOrAlias, out key!);

    private static Dictionary<string, KeyboardKey> Create()
    {
        var keys = new Dictionary<string, KeyboardKey>(StringComparer.OrdinalIgnoreCase);

        void Add(string name, string label, string? symbol = null, bool symbolOnly = false, params string[] aliases)
        {
            var key = new KeyboardKey(name, label, symbol, symbolOnly);
            keys.Add(name, key);
            foreach (var alias in aliases) keys.Add(alias, key);
        }

        void AddPlain(string name, string label, params string[] aliases) => Add(name, label, aliases: aliases);

        for (var digit = '0'; digit <= '9'; digit++) AddPlain(digit.ToString(), digit.ToString());
        for (var letter = 'a'; letter <= 'z'; letter++) AddPlain(letter.ToString(), char.ToUpperInvariant(letter).ToString());

        // Punctuation
        Add("space", "Space", "␣", aliases: "spc");
        AddPlain("backslash", "\\");
        AddPlain("bar", "|", "pipe");
        AddPlain("brace-left", "{", "open-brace");
        AddPlain("brace-right", "}", "close-brace");
        AddPlain("bracket-left", "[", "open-bracket");
        AddPlain("bracket-right", "]", "close-bracket");
        AddPlain("colon", ":");
        AddPlain("comma", ",");
        AddPlain("double-quote", "\"", "dblquote");
        AddPlain("equal", "=");
        AddPlain("exclam", "!", "exclamation");
        AddPlain("grave", "`", "grave-accent");
        AddPlain("greater", ">", "greater-than", "gt");
        AddPlain("less", "<", "less-than", "lt");
        AddPlain("minus", "-", "hyphen");
        AddPlain("period", ".");
        AddPlain("plus", "+");
        AddPlain("question", "?", "question-mark");
        AddPlain("semicolon", ";");
        AddPlain("single-quote", "'");
        AddPlain("slash", "/");
        AddPlain("tilde", "~");
        AddPlain("underscore", "_");

        // Navigation and editing
        Add("arrow-up", "Up", "↑", true, "up");
        Add("arrow-down", "Down", "↓", true, "down");
        Add("arrow-left", "Left", "←", true, "left");
        Add("arrow-right", "Right", "→", true, "right");
        Add("page-up", "Page Up", "⇞", aliases: new[] { "prior", "pg-up" });
        Add("page-down", "Page Down", "⇟", aliases: new[] { "next", "page-dn", "pg-dn" });
        Add("home", "Home", "↖");
        Add("end", "End", "↘");
        Add("tab", "Tab", "⇥", aliases: "tabulator");
        Add("backtab", "Back Tab", "⇤", aliases: "bktab");
        Add("backspace", "Backspace", "⌫", aliases: new[] { "back", "bksp" });
        Add("delete", "Del", "⌦", aliases: "del");
        AddPlain("insert", "Ins", "ins");
        AddPlain("break", "Break", "cancel");
        Add("caps-lock", "Caps Lock", "⇪", aliases: new[] { "capital", "cplk" });
        AddPlain("clear", "Clear", "clr");
        Add("eject", "Eject", "⏏" + TextStyle);
        Add("enter", "Enter", "⏎", aliases: "return");
        Add("escape", "Esc", "⎋", aliases: "esc");
        AddPlain("help", "Help");
        AddPlain("print-screen", "Print Screen", "prtsc");
        AddPlain("scroll-lock", "Scroll Lock", "scroll");

        // Number pad
        for (var digit = 0; digit <= 9; digit++) AddPlain($"num{digit}", $"Num {digit}");
        AddPlain("num-asterisk", "Num *", "multiply");
        AddPlain("num-clear", "Num Clear");
        AddPlain("num-delete", "Num Del", "num-del");
        AddPlain("num-equal", "Num =");
        AddPlain("num-lock", "Num Lock", "numlk", "numlock");
        AddPlain("num-minus", "Num -", "subtract");
        AddPlain("num-plus", "Num +", "add");
        AddPlain("num-separator", "Num .", "decimal", "separator");
        AddPlain("num-slash", "Num /", "divide");
        AddPlain("num-enter", "Num Enter");

        // Modifiers
        AddPlain("alt", "Alt");
        AddPlain("left-alt", "Left Alt", "lalt");
        AddPlain("right-alt", "Right Alt", "ralt");
        AddPlain("alt-graph", "AltGr", "altgr");
        Add("command", "Cmd", "⌘", aliases: "cmd");
        Add("left-command", "Left Command", "⌘", aliases: new[] { "lcommand", "lcmd", "left-cmd" });
        Add("right-command", "Right Command", "⌘", aliases: new[] { "rcommand", "rcmd", "right-cmd" });
        Add("control", "Ctrl", "⌃", aliases: "ctrl");
        Add("left-control", "Left Ctrl", "⌃", aliases: new[] { "lcontrol", "lctrl", "left-ctrl" });
        Add("right-control", "Right Ctrl", "⌃", aliases: new[] { "rcontrol", "rctrl", "right-ctrl" });
        AddPlain("function", "Fn", "fn");
        Add("globe", "Globe", "🌐" + TextStyle);
        AddPlain("meta", "Meta");
        AddPlain("left-meta", "Left Meta", "lmeta");
        AddPlain("right-meta", "Right Meta", "rmeta");
        Add("option", "Option", "⌥", aliases: "opt");
        Add("left-option", "Left Option", "⌥", aliases: new[] { "loption", "lopt", "left-opt" });
        Add("right-option", "Right Option", "⌥", aliases: new[] { "roption", "ropt", "right-opt" });
        Add("shift", "Shift", "⇧");
        Add("left-shift", "Left Shift", "⇧", aliases: "lshift");
        Add("right-shift", "Right Shift", "⇧", aliases: "rshift");
        AddPlain("super", "Super");
        AddPlain("left-super", "Left Super", "lsuper");
        AddPlain("right-super", "Right Super", "rsuper");
        Add("windows", "Win", "⊞", aliases: "win");
        Add("left-windows", "Left Win", "⊞", aliases: new[] { "lwindows", "left-win", "lwin" });
        Add("right-windows", "Right Win", "⊞", aliases: new[] { "rwindows", "right-win", "rwin" });

        // Function keys
        for (var number = 1; number <= 24; number++) AddPlain($"f{number}", $"F{number}");

        // Browser, media, and other special keys
        AddPlain("browser-back", "Browser Back");
        AddPlain("browser-favorites", "Browser Favorites", "favorites");
        AddPlain("browser-forward", "Browser Forward", "forward");
        AddPlain("browser-home", "Browser Home");
        AddPlain("browser-refresh", "Browser Refresh", "refresh");
        AddPlain("browser-search", "Browser Search", "search");
        AddPlain("browser-stop", "Browser Stop");
        AddPlain("copy", "Copy");
        AddPlain("context-menu", "Menu", "apps", "menu");
        AddPlain("fingerprint", "Fingerprint");
        AddPlain("mail", "Mail", "launch-mail");
        AddPlain("media", "Media", "launch-media");
        Add("media-next-track", "Next Track", "⏭" + TextStyle, aliases: "next-track");
        Add("media-pause", "Pause", "⏸" + TextStyle, aliases: "pause");
        Add("media-play", "Play", "▶" + TextStyle, aliases: "play");
        Add("media-play-pause", "Play/Pause", "⏯" + TextStyle, aliases: "play-pause");
        Add("media-prev-track", "Previous Track", "⏮" + TextStyle, aliases: "prev-track");
        Add("media-stop", "Stop", "⏹" + TextStyle, aliases: "stop");
        Add("power", "Power", "⏻");
        AddPlain("print", "Print");
        AddPlain("reset", "Reset");
        AddPlain("select", "Select");
        AddPlain("sleep", "Sleep");
        AddPlain("volume-down", "Volume Down", "vol-down");
        AddPlain("volume-mute", "Mute", "mute");
        AddPlain("volume-up", "Volume Up", "vol-up");
        AddPlain("zoom", "Zoom");
        AddPlain("left-button", "Left Button", "lbutton");
        AddPlain("middle-button", "Middle Button", "mbutton");
        AddPlain("right-button", "Right Button", "rbutton");
        AddPlain("x-button1", "X Button 1", "xbutton1");
        AddPlain("x-button2", "X Button 2", "xbutton2");

        return keys;
    }
}
