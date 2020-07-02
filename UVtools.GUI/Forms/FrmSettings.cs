﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using UVtools.Core;
using UVtools.GUI.Properties;

namespace UVtools.GUI.Forms
{
    public partial class FrmSettings : Form
    {
        public FrmSettings()
        {
            InitializeComponent();

            var fileFormats = new List<string>
            {
                FileFormat.AllSlicerFiles.Replace("*", string.Empty)
            };
            fileFormats.AddRange(from format in FileFormat.AvaliableFormats from extension in format.FileExtensions select $"{extension.Description} (.{extension.Extension})");
            cbDefaultOpenFileExtension.Items.AddRange(fileFormats.ToArray());

            Init();
        }

        public void Init()
        {
            try
            {
                cbCheckForUpdatesOnStartup.Checked = Settings.Default.CheckForUpdatesOnStartup;
                cbStartMaximized.Checked = Settings.Default.StartMaximized;
                cbDefaultOpenFileExtension.SelectedIndex = Settings.Default.DefaultOpenFileExtension;

                btnPreviousNextLayerColor.BackColor = Settings.Default.PreviousNextLayerColor;
                btnPreviousLayerColor.BackColor = Settings.Default.PreviousLayerColor;
                btnNextLayerColor.BackColor = Settings.Default.NextLayerColor;
                btnIslandColor.BackColor = Settings.Default.IslandColor;
                btnResinTrapColor.BackColor = Settings.Default.ResinTrapColor;
                btnTouchingBoundsColor.BackColor = Settings.Default.TouchingBoundsColor;

                btnOutlinePrintVolumeBoundsColor.BackColor = Settings.Default.OutlinePrintVolumeBoundsColor;
                btnOutlineLayerBoundsColor.BackColor = Settings.Default.OutlineLayerBoundsColor;
                btnOutlineHollowAreasColor.BackColor = Settings.Default.OutlineHollowAreasColor;

                nmOutlinePrintVolumeBoundsLineThickness.Value = Settings.Default.OutlinePrintVolumeBoundsLineThickness;
                nmOutlineLayerBoundsLineThickness.Value = Settings.Default.OutlineLayerBoundsLineThickness;
                nmOutlineHollowAreasLineThickness.Value = Settings.Default.OutlineHollowAreasLineThickness;

                cbOutlinePrintVolumeBounds.Checked = Settings.Default.OutlinePrintVolumeBounds;
                cbOutlineLayerBounds.Checked = Settings.Default.OutlineLayerBounds;
                cbOutlineHollowAreas.Checked = Settings.Default.OutlineHollowAreas;

                cbLayerAutoRotateBestView.Checked = Settings.Default.LayerAutoRotateBestView;
                cbLayerZoomToFit.Checked = Settings.Default.LayerZoomToFit;
                cbZoomToFitPrintVolumeBounds.Checked = Settings.Default.ZoomToFitPrintVolumeBounds;
                cbLayerDifferenceDefault.Checked = Settings.Default.LayerDifferenceDefault;
                cbComputeIssuesOnLoad.Checked = Settings.Default.ComputeIssuesOnLoad;
                cbComputeIslands.Checked = Settings.Default.ComputeIslands;
                cbComputeResinTraps.Checked = Settings.Default.ComputeResinTraps;
                cbAutoComputeIssuesClickOnTab.Checked = Settings.Default.AutoComputeIssuesClickOnTab;

                nmIslandBinaryThreshold.Value = Settings.Default.IslandBinaryThreshold;
                nmIslandRequiredAreaToProcessCheck.Value = Settings.Default.IslandRequiredAreaToProcessCheck;
                nmIslandRequiredPixelBrightnessToProcessCheck.Value = Settings.Default.IslandRequiredPixelBrightnessToProcessCheck;
                nmIslandRequiredPixelsToSupport.Value = Settings.Default.IslandRequiredPixelsToSupport;
                nmIslandRequiredPixelBrightnessToSupport.Value = Settings.Default.IslandRequiredPixelBrightnessToSupport;

                nmResinTrapBinaryThreshold.Value = Settings.Default.ResinTrapBinaryThreshold;
                nmResinTrapRequiredAreaToProcessCheck.Value = Settings.Default.ResinTrapRequiredAreaToProcessCheck;
                nmResinTrapRequiredBlackPixelsToDrain.Value = Settings.Default.ResinTrapRequiredBlackPixelsToDrain;
                nmResinTrapMaximumPixelBrightnessToDrain.Value = Settings.Default.ResinTrapMaximumPixelBrightnessToDrain;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to use current settings, a reset will be performed.\n{ex.Message}",
                    "Unable to use current settings", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Settings.Default.Reset();
                Settings.Default.Save();
                Init();
            }
        }

        private void EventClick(object sender, EventArgs e)
        {
            if (
                ReferenceEquals(sender, btnPreviousNextLayerColor) ||
                ReferenceEquals(sender, btnPreviousLayerColor) ||
                ReferenceEquals(sender, btnNextLayerColor) ||
                ReferenceEquals(sender, btnIslandColor) ||
                ReferenceEquals(sender, btnResinTrapColor) ||
                ReferenceEquals(sender, btnTouchingBoundsColor) ||
                ReferenceEquals(sender, btnOutlinePrintVolumeBoundsColor) ||
                ReferenceEquals(sender, btnOutlineLayerBoundsColor) ||
                ReferenceEquals(sender, btnOutlineHollowAreasColor)
                )
            {
                Button btn = sender as Button;
                colorDialog.Color = btn.BackColor;
                if (colorDialog.ShowDialog() != DialogResult.OK) return;
                
                btn.BackColor = colorDialog.Color;

                return;

            }

            if (ReferenceEquals(sender, btnReset))
            {
                if (MessageBox.Show("Are you sure you want to reset the settings to the default values?",
                        "Reset settings?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) !=
                    DialogResult.Yes) return;

                Settings.Default.Reset();
                Init();

                return;
            }
            
            if (ReferenceEquals(sender, btnSave))
            {
                Settings.Default.CheckForUpdatesOnStartup = cbCheckForUpdatesOnStartup.Checked;
                Settings.Default.StartMaximized = cbStartMaximized.Checked;
                Settings.Default.DefaultOpenFileExtension = (byte) cbDefaultOpenFileExtension.SelectedIndex;

                Settings.Default.PreviousNextLayerColor = btnPreviousNextLayerColor.BackColor;
                Settings.Default.PreviousLayerColor = btnPreviousLayerColor.BackColor;
                Settings.Default.NextLayerColor = btnNextLayerColor.BackColor;
                Settings.Default.IslandColor = btnIslandColor.BackColor;
                Settings.Default.ResinTrapColor = btnResinTrapColor.BackColor;
                Settings.Default.TouchingBoundsColor = btnTouchingBoundsColor.BackColor;

                Settings.Default.OutlinePrintVolumeBoundsColor = btnOutlinePrintVolumeBoundsColor.BackColor;
                Settings.Default.OutlineLayerBoundsColor = btnOutlineLayerBoundsColor.BackColor;
                Settings.Default.OutlineHollowAreasColor = btnOutlineHollowAreasColor.BackColor;

                Settings.Default.OutlinePrintVolumeBoundsLineThickness = (byte) nmOutlinePrintVolumeBoundsLineThickness.Value;
                Settings.Default.OutlineLayerBoundsLineThickness = (byte) nmOutlineLayerBoundsLineThickness.Value;
                Settings.Default.OutlineHollowAreasLineThickness = (sbyte) nmOutlineHollowAreasLineThickness.Value;

                Settings.Default.OutlinePrintVolumeBounds = cbOutlinePrintVolumeBounds.Checked;
                Settings.Default.OutlineLayerBounds = cbOutlineLayerBounds.Checked;
                Settings.Default.OutlineHollowAreas = cbOutlineHollowAreas.Checked;

                Settings.Default.LayerAutoRotateBestView = cbLayerAutoRotateBestView.Checked;
                Settings.Default.LayerZoomToFit = cbLayerZoomToFit.Checked;
                Settings.Default.ZoomToFitPrintVolumeBounds = cbZoomToFitPrintVolumeBounds.Checked;
                Settings.Default.LayerDifferenceDefault = cbLayerDifferenceDefault.Checked;
                Settings.Default.ComputeIssuesOnLoad = cbComputeIssuesOnLoad.Checked;
                Settings.Default.ComputeIslands = cbComputeIslands.Checked;
                Settings.Default.ComputeResinTraps = cbComputeResinTraps.Checked;
                Settings.Default.AutoComputeIssuesClickOnTab = cbAutoComputeIssuesClickOnTab.Checked;

                Settings.Default.IslandBinaryThreshold = (byte)nmIslandBinaryThreshold.Value;
                Settings.Default.IslandRequiredAreaToProcessCheck = (byte) nmIslandRequiredAreaToProcessCheck.Value;
                Settings.Default.IslandRequiredPixelBrightnessToProcessCheck = (byte)nmIslandRequiredPixelBrightnessToProcessCheck.Value;
                Settings.Default.IslandRequiredPixelsToSupport = (byte)nmIslandRequiredPixelsToSupport.Value;
                Settings.Default.IslandRequiredPixelBrightnessToSupport = (byte)nmIslandRequiredPixelBrightnessToSupport.Value;

                Settings.Default.ResinTrapBinaryThreshold = (byte) nmResinTrapBinaryThreshold.Value;
                Settings.Default.ResinTrapRequiredAreaToProcessCheck = (byte)nmResinTrapRequiredAreaToProcessCheck.Value;
                Settings.Default.ResinTrapRequiredBlackPixelsToDrain = (byte)nmResinTrapRequiredBlackPixelsToDrain.Value;
                Settings.Default.ResinTrapMaximumPixelBrightnessToDrain = (byte)nmResinTrapMaximumPixelBrightnessToDrain.Value;

                Settings.Default.Save();
                DialogResult = DialogResult.OK;
                return;
            }
        }
    }
}
