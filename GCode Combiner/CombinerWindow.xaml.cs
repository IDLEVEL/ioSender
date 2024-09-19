using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.IO;
using System.Windows.Input;
using System.Windows.Shell;

namespace GCode_Combiner
{
    /// <summary>
    /// Логика взаимодействия для CombinerWindow.xaml
    /// </summary>
    public partial class CombinerWindow : Window
    {
        private ListView _list_view;
        private TextBox _gcode_box;

        private IList<FileItem> _file_items = new ObservableCollection<FileItem>();

        public CombinerWindow()
        {
            InitializeComponent();

            _list_view = FindName("FileItems") as ListView;
            _gcode_box = FindName("GCodeViewer") as TextBox;

            _list_view.ItemsSource = _file_items;

            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            Initialize();
        }

        void Initialize()
        {
        }

        private String ReadFileIfExistsOrEmpty(string path)
        {
            if (File.Exists(path))
                return File.ReadAllText(path);

            return null;
        }

        static string[] CyrilicToLatinL =
            "a,b,v,g,d,e,zh,z,i,j,k,l,m,n,o,p,r,s,t,u,f,kh,c,ch,sh,sch,j,y,j,e,yu,ya".Split(',');
        static string[] CyrilicToLatinU =
            "A,B,V,G,D,E,Zh,Z,I,J,K,L,M,N,O,P,R,S,T,U,F,Kh,C,Ch,Sh,Sch,J,Y,J,E,Yu,Ya".Split(',');

        public static string CyrilicToLatin(string s)
        {
            var sb = new StringBuilder((int)(s.Length * 1.5));
            foreach (char c in s)
            {
                if (c >= '\x430' && c <= '\x44f') sb.Append(CyrilicToLatinL[c - '\x430']);
                else if (c >= '\x410' && c <= '\x42f') sb.Append(CyrilicToLatinU[c - '\x410']);
                else if (c == '\x401') sb.Append("Yo");
                else if (c == '\x451') sb.Append("yo");
                else sb.Append(c);
            }
            return sb.ToString();
        }

        private void SaveGcode(object sender, EventArgs e)
        {
            var fileDialog = new SaveFileDialog();
            fileDialog.Filter = "GCode Files|*.*";
            fileDialog.Title = "Select GCode file(s)...";
            fileDialog.FileName = "output.gcode";

            var success = fileDialog.ShowDialog();

            if (success.GetValueOrDefault())
            {
                var gcodeDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../gcode");

                var allGcode = new System.Text.StringBuilder();

                var appendGcode = ReadFileIfExistsOrEmpty(Path.Combine(gcodeDir, "append.gcode"));
                var prependGcode = ReadFileIfExistsOrEmpty(Path.Combine(gcodeDir, "prepend.gcode"));
                var concatGcode = ReadFileIfExistsOrEmpty(Path.Combine(gcodeDir, "concat.gcode"));

                var selected_file_items = _file_items.Where(f => f.Selected).ToArray();

                for (int i = 0; i < selected_file_items.Length; i++)
                {
                    var item = selected_file_items[i];

                    if(i == 0 && prependGcode != null)
                    {
                        allGcode.AppendLine(prependGcode.Replace("{fileName}", CyrilicToLatin(item.FileName)));
                    }

                    var lines = File.ReadAllText(selected_file_items[i].FilePath);

                    allGcode.AppendLine(lines.Replace("M30", ""));

                    if (i < selected_file_items.Length - 1)
                    {
                        allGcode.AppendLine(concatGcode.Replace("{fileName}", CyrilicToLatin(selected_file_items[i + 1].FileName)));
                    }
                }

                if (appendGcode != null)
                {
                    allGcode.AppendLine(appendGcode);
                }

                allGcode.AppendLine("M30");

                File.WriteAllText(fileDialog.FileName, allGcode.ToString());
            }
        }

        private void DeleteFileItem(object sender, EventArgs args)
        {
            var fileItem = (FileItem)(((Button)sender).DataContext);

            _file_items.Remove(fileItem);
        }

        private Button previousViewedButton;

        private void ViewFileItem(object sender, EventArgs args)
        {
            var button = ((Button)sender);
            var fileItem = (FileItem)(button.DataContext);

            _gcode_box.Dispatcher.Invoke(() =>
                _gcode_box.Text = File.ReadAllText(fileItem.FilePath));

            if (previousViewedButton != null)
                previousViewedButton.Background = new SolidColorBrush(Colors.White);

            previousViewedButton = button;
            button.Background = new SolidColorBrush(Colors.Red);
        }

        private void OpenFile(object sender, EventArgs e)
        {
            var fileDialog = new OpenFileDialog();
            fileDialog.Filter = "GCode Files (*.*)|*.*";
            fileDialog.Title = "Select GCode file(s)...";
            fileDialog.Multiselect = true; // Set to false or never mention this line for single file select

            var success = fileDialog.ShowDialog();

            if (success.GetValueOrDefault())
            {
                foreach (var file in fileDialog.FileNames.Reverse())
                    _file_items.Add(new FileItem { FilePath = file, Selected = true });
            }
        }


        private void ImagePanel_Drop(object sender, DragEventArgs e)
        {

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Note that you can have more than one file.
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                foreach (var file in files)
                    _file_items.Add(new FileItem { FilePath = file, Selected = true });
            }
        }
        
    }
}
