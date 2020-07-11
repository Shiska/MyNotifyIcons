using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Microsoft.Win32;

namespace MyNofityIcons
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length != 0)
            {
                switch (args[0])
                {
                    case "--autostart":
                        foreach (var (executable, _) in NofityIconSetting.executable)
                        {
                            NofityIconRun.Start(executable, true);
                        }
                        return;
                    case "--start":
                        new NofityIconRun(args[1], hidden: Boolean.Parse(args[2]));

                        return;
                }
            }
            Application.Run(new NofityIconForm());
        }
    }

    public class NofityIconForm : Form
    {
        private TableLayoutPanel layout;
        private Button addButton;

        public NofityIconForm()
        {
            this.Text = "MyNofityIcons";
            this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            this.AutoSize = true;
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.MaximizeBox = false;

            this.layout = new TableLayoutPanel();
            this.layout.AutoSize = true;
            this.layout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.layout.ColumnCount = 4;
            this.layout.GrowStyle = TableLayoutPanelGrowStyle.AddRows;

            Label pathLabel = new Label();

            pathLabel.Text = "Executable";
            pathLabel.AutoSize = true;
            pathLabel.Anchor = AnchorStyles.Right;

            CheckBox autostartCB = new CheckBox();

            autostartCB.AutoSize = true;
            autostartCB.Text = "Autostart with Microsoft Windows";
            autostartCB.Checked = NofityIconSetting.autostart;
            autostartCB.Anchor = AnchorStyles.Left;
            autostartCB.CheckedChanged += new EventHandler((sender, e) => {
                NofityIconSetting.autostart = autostartCB.Checked;
            });

            this.addButton = new Button();

            this.addButton.Text = "Add";
            this.addButton.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            this.addButton.Click += new EventHandler(addButton_Click);

            this.layout.SetColumnSpan(autostartCB, 3);
            this.layout.SetColumnSpan(this.addButton, 4);

            this.layout.Controls.Add(pathLabel);
            this.layout.Controls.Add(autostartCB);
            this.layout.Controls.Add(this.addButton);

            this.Controls.Add(this.layout);

            foreach (var (executale, autostart) in NofityIconSetting.executable)
            {
                AddRow(executale, autostart);
            }
        }

        void addButton_Click(Object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    if (NofityIconSetting.isInExecutable(openFileDialog.FileName) == false)
                    {
                        AddRow(openFileDialog.FileName, false);

                        storeData();
                    }
                }
            }
        }

        void AddRow(string path, bool autostart)
        {
            TableLayoutControlCollection controls = this.layout.Controls;

            Label label = new Label();

            label.Text = path;
            label.AutoSize = true;
            label.Anchor = AnchorStyles.Right;

            CheckBox cb = new CheckBox();

            cb.AutoSize = true;
            cb.Text = "Autostart";
            cb.Checked = autostart;
            cb.Anchor = AnchorStyles.None;
            cb.CheckedChanged += new EventHandler(storeData);

            Button runButton = new Button();

            runButton.AutoSize = true;
            runButton.Text = "Run";
            runButton.Anchor = AnchorStyles.Left;
            runButton.Click += new EventHandler((sender, e) => {
                NofityIconRun.Start(path, false);
            });

            Button remButton = new Button();

            remButton.AutoSize = true;
            remButton.Text = "Remove";
            remButton.Anchor = AnchorStyles.Left;
            remButton.Click += new EventHandler((sender, e) => {
                this.SuspendLayout();

                controls.Remove(label);
                controls.Remove(cb);
                controls.Remove(runButton);
                controls.Remove(remButton);

                this.ResumeLayout();
            });
            remButton.Click += new EventHandler(storeData);

            this.SuspendLayout();

            controls.Add(label);
            controls.Add(cb);
            controls.Add(runButton);
            controls.Add(remButton);
            controls.Add(this.addButton);

            this.ResumeLayout();
        }

        void storeData(object sender = null, EventArgs e = null)
        {
            TableLayoutControlCollection controls = this.layout.Controls;

            var executable = new List<(string, bool)>();

            for (int i = controls.Count - 1; (i -= 4) >= 2;)
            {
                executable.Add((
                    ((Label)controls[i]).Text,
                    ((CheckBox)controls[i + 1]).Checked
                ));
            }
            executable.Reverse();

            NofityIconSetting.executable = executable;
        }
    }

    public class NofityIconSetting
    {
        const string key = "NofityIconAutostart";

        public static bool autostart
        {
            get { return Properties.Settings.Default.Autostart; }
            set
            {
                RegistryKey rk = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);

                if ((Properties.Settings.Default.Autostart = value))
                {
                    rk.SetValue(key, "\"" + Path.GetFullPath(Application.ExecutablePath) + "\" --autostart");
                }
                else
                {
                    rk.DeleteValue(key, false);
                }
                Properties.Settings.Default.Save();
            }
        }

        public static bool isInExecutable(string executable)
        {
            return Properties.Settings.Default.Executable.Contains(executable);
        }

        public static IEnumerable<(string, bool)> executable
        {
            get
            {
                char[] rowSeparators = { '|' };
                char[] colSeparators = { '(', ',', ')' };

                string[] executable = Properties.Settings.Default.Executable.Split(rowSeparators, StringSplitOptions.RemoveEmptyEntries);

                foreach (string row in executable)
                {
                    string[] column = row.Split(colSeparators, StringSplitOptions.RemoveEmptyEntries);

                    yield return (column[0], Boolean.Parse(column[1]));
                }
            }
            set
            {
                Properties.Settings.Default.Executable = String.Join("|", value.ToArray());
                Properties.Settings.Default.Save();
            }
        }
    }

    public class NofityIconRun
    {
        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;

        [DllImport("User32")]
        private static extern int ShowWindow(int hwnd, int nCmdShow);
        [DllImport("User32")]
        public static extern int SetForegroundWindow(int hwnd);

        public NofityIconRun(string executable, string arguments = "", int timeout = 5, bool hidden = false)
        {
            NotifyIcon icon = new NotifyIcon();

            icon.Text = Path.ChangeExtension(executable, null);
            icon.Visible = true;

            if (File.Exists(executable))
            {
                icon.Icon = Icon.ExtractAssociatedIcon(executable);

                Process process = Process.Start(executable, arguments);

                if (process != null)
                {
                    int hWnd = 0;

                    icon.MouseDoubleClick += new MouseEventHandler((sender, e) => {
                        if ((hidden = !hidden))
                        {
                            ShowWindow(hWnd, SW_HIDE);
                        }
                        else
                        {
                            ShowWindow(hWnd, SW_SHOW);
                            SetForegroundWindow(hWnd);
                        }
                    });

                    process.EnableRaisingEvents = true;
                    process.Exited += new EventHandler((sender, e) => { Application.Exit(); });

                    while ((hWnd = (int)process.MainWindowHandle) == 0)
                    {
                        Thread.Yield();
                    }
                    if (hidden)
                    {
                        ShowWindow(hWnd, SW_HIDE);
                    }
                    Application.Run();
                }
                else
                {
                    icon.ShowBalloonTip(timeout, icon.Text, "Failed to start process!", ToolTipIcon.Error);

                    Thread.Sleep(timeout * 1000);
                }
            }
            else
            {
                icon.ShowBalloonTip(timeout, icon.Text, executable + " not found!", ToolTipIcon.Error);

                Thread.Sleep(timeout * 1000);
            }
        }

        static private string executablePath = Path.GetFullPath(Application.ExecutablePath);

        public static Process Start(string executable, bool hidden)
        {
            return Process.Start(executablePath, "--start \"" + executable + "\" " + hidden.ToString());
        }
    }
}