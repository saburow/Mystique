﻿using System.Windows.Controls;
using System.Windows.Input;
using Inscribe.Configuration.KeyAssignment;

namespace Mystique.Views.PartBlocks.MainBlock
{
    /// <summary>
    /// ColumnOwner.xaml の相互作用ロジック
    /// </summary>
    public partial class ColumnOwner : UserControl
    {
        public ColumnOwner()
        {
            InitializeComponent();
        }

        private void UserControl_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            KeyAssign.HandlePreviewEvent(e, AssignRegion.Timeline);
            System.Diagnostics.Debug.WriteLine(e.Key);
        }

        private void UserControl_KeyDown(object sender, KeyEventArgs e)
        {
            KeyAssign.HandleEvent(e, AssignRegion.Timeline);
            System.Diagnostics.Debug.WriteLine(e.Key);
        }
    }
}
