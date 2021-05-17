/*
 * AppConfigView.xaml.cs - part of CNC Controls library for Grbl
 *
 * v0.33 / 2021-05-15 / Io Engineering (Terje Io)
 *
 */
/*

Copyright (c) 2020-2021, Io Engineering (Terje Io)
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

using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using CNC.Core;

namespace CNC.Controls
{
    public partial class AppConfigView : UserControl, ICNCView
    {
        private UIViewModel model;
        private GrblViewModel grblmodel;

        public AppConfigView()
        {
            InitializeComponent();
        }

        ObservableCollection<UserControl> ConfigControls { get { return model == null ? null : model.ConfigControls;  } }

        #region Methods and properties required by CNCView interface

        public ViewType ViewType { get { return ViewType.AppConfig; } }
        public bool CanEnable { get { return true; } }

        public void Activate(bool activate, ViewType chgMode)
        {
            if(activate) foreach(var control in model.ConfigControls) // TODO: use callback!
            {
                if (control is JogConfigControl) {
                    if (GrblSettings.GetString(GrblSetting.JogStepSpeed) != null)
                        control.Visibility = Visibility.Hidden;
                    else
                        (control as JogConfigControl).IsGrbl = !GrblInfo.IsGrblHAL;
                } else if (control is ICameraConfig && model.Camera != null && !model.Camera.HasCamera)
                    control.Visibility = Visibility.Collapsed;
            }
            grblmodel.Message = activate ? "A restart is required after changing settings!" : string.Empty;
        }

        public void CloseFile()
        {
        }

        public void Setup(UIViewModel model, AppConfig profile)
        {
            if (this.model == null)
            {
                this.model = model;
                grblmodel = DataContext as GrblViewModel;
                DataContext = profile.Base;
                xx.ItemsSource = model.ConfigControls;
                model.ConfigControls.Add(new BasicConfigControl());
                model.ConfigControls.Add(new StripGCodeConfigControl());
                model.ConfigControls.Add(new JogConfigControl());
            }
        }

        #endregion

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            AppConfig.Settings.Save();
        }
    }
}
