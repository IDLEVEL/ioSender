/*
 * MDIControl.xaml.cs - part of CNC Controls library for Grbl
 *
 * v0.44 / 2023-10-07 / Io Engineering (Terje Io)
 *
 */

/*

Copyright (c) 2018-2023, Io Engineering (Terje Io)
All rights reserved.

Redistribution and use in source and binary forms, with or without modification,
are permitted provided that the following conditions are met:

· Redistributions of source code must retain the above copyright notice, this
list of conditions and the following disclaimer.

· Redistributions in binary form must reproduce the above copyright notice, this
list of conditions and the following disclaimer in the documentation and/or
other materials provided with the distribution.

· Neither the name of the copyright holder nor the names of its contributors may
be used to endorse or promote products derived from this software without
specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON
ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

*/

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.ComponentModel;
using CNC.Core;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Threading;
using System.IO;
using System.Net;

namespace CNC.Controls
{
    public partial class MDIControl : UserControl
    {
        public MDIControl()
        {
            InitializeComponent();

            Commands = new ObservableCollection<string>();
        }

        public new bool IsFocused { get { return txtMDI.IsKeyboardFocusWithin; } }

        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(nameof(Command), typeof(string), typeof(MDIControl), new PropertyMetadata(""));
        public string Command
        {
            get { return (string)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        public static readonly DependencyProperty CommandsProperty = DependencyProperty.Register(nameof(Commands), typeof(ObservableCollection<string>), typeof(MDIControl));
        public ObservableCollection<string> Commands
        {
            get { return (ObservableCollection<string>)GetValue(CommandsProperty); }
            set { SetValue(CommandsProperty, value); }
        }

        private void OnDataContextPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is GrblViewModel) switch (e.PropertyName)
            {
                case nameof(GrblViewModel.MDIText):
                    var txt = (sender as GrblViewModel).MDIText;
                    if (!string.IsNullOrEmpty(txt) && !Commands.Contains(txt))
                        Commands.Insert(0, txt);
                    Command = txt;
                    break;
            }
        }

        private void txtMDI_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Return && (DataContext as GrblViewModel).MDICommand.CanExecute(null))
            {
                string cmd = (sender as ComboBox).Text;
                var model = DataContext as GrblViewModel;
                if (!string.IsNullOrEmpty(cmd) && (Commands.Count == 0 || Commands[0] != cmd))
                    Commands.Insert(0, cmd);
                if (model.GrblError != 0)
                    model.ExecuteCommand("");
                model.MDICommand.Execute(cmd);
                (sender as ComboBox).SelectedIndex = -1;
            }
        }

        private void txtMDI_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var cmbTextBox = (TextBox)(sender as ComboBox).Template.FindName("PART_EditableTextBox", (sender as ComboBox));
            if (cmbTextBox != null)
            {
                cmbTextBox.Focus();
                cmbTextBox.CaretIndex = cmbTextBox.Text.Length;
            }
        }

        private void Send_Click(object sender, RoutedEventArgs e)
        {
            if ((DataContext as GrblViewModel).GrblError != 0)
                (DataContext as GrblViewModel).ExecuteCommand("");

            if (!string.IsNullOrEmpty(Command) && !Commands.Contains(Command))
                Commands.Insert(0, Command);
        }

        private void MDIControl_Loaded(object sender, RoutedEventArgs e)
        {
            var mdi = txtMDI.Template.FindName("PART_EditableTextBox", txtMDI) as TextBox;
            if(mdi != null)
                mdi.Tag = "MDI";
            if(DataContext != null)
                (DataContext as GrblViewModel).PropertyChanged += OnDataContextPropertyChanged;
        }

        private void Upload_And_Run(object sender, RoutedEventArgs e)
        {
            string IP = null;

            var ipRegex = new System.Text.RegularExpressions.Regex("IP=(?<IP>\\d+\\.\\d+\\.(\\d+).\\d+)");

            foreach (string opt in GrblInfo.SystemInfo)
            {
                var match = ipRegex.Match(opt);

                if(match.Success)
                {
                    IP = match.Groups["IP"].Value;
                    break;
                }
            }

            if (IP == null)
            {
                MessageBox.Show("Machine IP not found");
                return;
            }

            var _ = this;

            var gcode = GCode.File.GetGcodeAsString();

            Task.Run(() =>
            {
                var fileName = "\\TEMP.GCODE";

                if (_.UploadFile("\\TEMP.GCODE", gcode))
                {
                    if (MessageBox.Show($"Run uploaded {fileName} file?", "Sure?",
                        MessageBoxButton.YesNo, MessageBoxImage.Question,
                        MessageBoxResult.No) == MessageBoxResult.Yes)
                    {
                        Comms.com.WriteCommand($"[ESP220]{fileName}\n");
                    }
                }
            });
        }

        public bool UploadFile(string fileName, String contentData)
        {
            this.Dispatcher.Invoke(() => uploadStatus.Content = "Uploading..");

            var boundary = "----WebKitFormBoundaryHEwnGACfAY4a1D2c";

            var content_src = $"--{boundary}\r\nContent-Disposition: form-data; name=\"path\"\r\n\r\n/\r\n" +
            $"--{boundary}\r\nContent-Disposition: form-data; name=\"/{fileName}S\"\r\n\r\n{contentData.Length}\r\n" +
            $"--{boundary}\r\nContent-Disposition: form-data; name=\"myfile[]\"; filename=\"/{fileName}\"\r\n" +
            $"Content-Type: application/octet-stream\r\n\r\n";

            // Create the request and set parameters
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://192.168.1.71/upload");
            request.ContentType = $"multipart/form-data; boundary={boundary}";

            request.Method = "POST";
            request.KeepAlive = true;

            request.Credentials = System.Net.CredentialCache.DefaultCredentials;

            var requestStream = new StreamWriter(request.GetRequestStream(), Encoding.ASCII);

            requestStream.Write(content_src);

            requestStream.Flush();

            int bytesRead;

            byte[] buffer = new byte[2048];

            using (var fileStream = new MemoryStream(Encoding.ASCII.GetBytes(contentData)))
            {
                while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    requestStream.BaseStream.Write(buffer, 0, bytesRead);
                }

                requestStream.BaseStream.Flush();

                fileStream.Close();
            }

            requestStream.Write("\r\n--" + boundary + "--\r\n");

            requestStream.Close();

            using (StreamReader reader = new StreamReader(request.GetResponse().GetResponseStream()))
            {
                var result = reader.ReadToEnd();

                if (result.Contains("\"status\":\"Ok\""))
                {
                    this.Dispatcher.Invoke(() => uploadStatus.Content = "Uploaded");

                    return true;
                }

                this.Dispatcher.Invoke(() => uploadStatus.Content = "Upload Fail");

                MessageBox.Show($"Upload Error: {reader.ReadToEnd()}");

                return false;
            };
        }
    }
}
