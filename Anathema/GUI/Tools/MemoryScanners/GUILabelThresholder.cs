﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using System.IO;
using Binarysharp.MemoryManagement;
using Binarysharp.MemoryManagement.Memory;

namespace Anathema
{
    public partial class GUILabelThresholder : DockContent, ILabelThresholderView
    {
        private LabelThresholderPresenter LabelThresholderPresenter;

        public GUILabelThresholder()
        {
            InitializeComponent();

            LabelThresholderPresenter = new LabelThresholderPresenter(this, new LabelThresholder());

            LabelThresholderPresenter.GatherData();
        }

        public void DisplayHistogram(SortedDictionary<dynamic, Int64> SortedDictionary)
        {
            foreach (KeyValuePair<dynamic, Int64> Item in SortedDictionary)
                LabelFrequencyChart.Series["Frequency"].Points.AddXY(Item.Key, Item.Value);

            UpdateTrackBars();
            UpdateSelection();
        }

        private void UpdateSelection()
        {
            for (Int32 Index = 0; Index < LabelFrequencyChart.Series["Frequency"].Points.Count; Index++)
            {
                if (Index < MinValueTrackBar.Value || Index > MaxValueTrackBar.Value)
                    LabelFrequencyChart.Series["Frequency"].Points[Index].Color = Color.Red;
                else
                    LabelFrequencyChart.Series["Frequency"].Points[Index].Color = Color.Blue;
            }
        }

        private void UpdateTrackBars()
        {
            MinValueTrackBar.Maximum = LabelFrequencyChart.Series["Frequency"].Points.Count;
            MaxValueTrackBar.Maximum = LabelFrequencyChart.Series["Frequency"].Points.Count;
            MaxValueTrackBar.Value = MaxValueTrackBar.Value;
        }

        #region Events

        private void MinValueTrackBar_Scroll(Object Sender, EventArgs E)
        {
            if (MinValueTrackBar.Value > MaxValueTrackBar.Value)
                MaxValueTrackBar.Value = MinValueTrackBar.Value;

            UpdateSelection();
        }

        private void MaxValueTrackBar_Scroll(Object Sender, EventArgs E)
        {
            if (MaxValueTrackBar.Value < MinValueTrackBar.Value)
                MinValueTrackBar.Value = MaxValueTrackBar.Value;

            UpdateSelection();
        }

        #endregion

    } // End class

} // End namespace