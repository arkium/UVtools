﻿/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Objects;
using UVtools.Core.Operations;
using UVtools.Core.PixelEditor;

namespace UVtools.Core
{
    public class LayerManager : BindableBase, IEnumerable<Layer>, IDisposable
    {
        #region Properties
        public FileFormat SlicerFile { get; set; }

        private Layer[] _layers;

        /// <summary>
        /// Layers List
        /// </summary>
        public Layer[] Layers
        {
            get => _layers;
            protected internal set
            {
                _layers = value;
                BoundingRectangle = Rectangle.Empty;
                SlicerFile.LayerCount = Count;
                SlicerFile.RequireFullEncode = true;
                if (value is null) return;
                SlicerFile.PrintTime = SlicerFile.PrintTimeComputed;
                SlicerFile.RebuildGCode();
            }
        }

        /// <summary>
        /// Gets the first layer
        /// </summary>
        public Layer FirstLayer => _layers?[0];

        /// <summary>
        /// Gets the last layer
        /// </summary>
        public Layer LastLayer => _layers?[^1];

        /// <summary>
        /// Gets the bounding rectangle of the object
        /// </summary>
        private Rectangle _boundingRectangle = Rectangle.Empty;

        /// <summary>
        /// Gets the bounding rectangle of the object
        /// </summary>
        public Rectangle BoundingRectangle
        {
            get => GetBoundingRectangle();
            set
            {
                RaiseAndSetIfChanged(ref _boundingRectangle, value);
                RaisePropertyChanged(nameof(BoundingRectangleMillimeters));
            }
        }

        /// <summary>
        /// Gets the bounding rectangle of the object in millimeters
        /// </summary>
        public RectangleF BoundingRectangleMillimeters
        {
            get
            {
                if (SlicerFile is null) return RectangleF.Empty;
                var pixelSize = SlicerFile.PixelSize;
                return new RectangleF(
                    (float)Math.Round(_boundingRectangle.X * pixelSize.Width, 2),
                    (float)Math.Round(_boundingRectangle.Y * pixelSize.Height, 2),
                    (float)Math.Round(_boundingRectangle.Width * pixelSize.Width, 2),
                    (float)Math.Round(_boundingRectangle.Height * pixelSize.Height, 2));
            }
        }
            

        /// <summary>
        /// Gets the layers count
        /// </summary>
        public uint Count => (uint) Layers.Length;

        public byte LayerDigits => (byte)Count.ToString().Length;

        /// <summary>
        /// Gets if any layer got modified, otherwise false
        /// </summary>
        public bool IsModified
        {
            get
            {
                for (uint i = 0; i < Count; i++)
                {
                    if (Layers[i].IsModified) return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Gets if all layers have same value parameters as global settings
        /// </summary>
        public bool AllLayersHaveGlobalParameters => Layers.Where(layer => layer is not null).All(layer => layer.HaveGlobalParameters);

        //public float LayerHeight => Layers[0].PositionZ;

        #endregion

        #region Constructors
        public LayerManager(uint layerCount, FileFormat slicerFile)
        {
            SlicerFile = slicerFile;
            _layers = new Layer[layerCount];
        }
        #endregion

        #region Indexers
        public Layer this[uint index]
        {
            get => Layers[index];
            set => AddLayer(index, value);
        }

        public Layer this[int index]
        {
            get => Layers[index];
            set => AddLayer((uint) index, value);
        }

        public Layer this[long index]
        {
            get => Layers[index];
            set => AddLayer((uint) index, value);
        }

        #endregion

        #region Numerators
        public IEnumerator<Layer> GetEnumerator()
        {
            return ((IEnumerable<Layer>)Layers).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        #endregion

        #region Static Methods
        /// <summary>
        /// Compress a layer from a <see cref="Stream"/>
        /// </summary>
        /// <param name="input"><see cref="Stream"/> to compress</param>
        /// <returns>Compressed byte array</returns>
        public static byte[] CompressLayer(Stream input)
        {
            return CompressLayer(input.ToArray());
        }

        /// <summary>
        /// Compress a layer from a byte array
        /// </summary>
        /// <param name="input">byte array to compress</param>
        /// <returns>Compressed byte array</returns>
        public static byte[] CompressLayer(byte[] input)
        {
            return input;
            /*using (MemoryStream output = new MemoryStream())
            {
                using (DeflateStream dstream = new DeflateStream(output, CompressionLevel.Optimal))
                {
                    dstream.Write(input, 0, input.Length);
                }
                return output.ToArray();
            }*/
        }

        /// <summary>
        /// Decompress a layer from a byte array
        /// </summary>
        /// <param name="input">byte array to decompress</param>
        /// <returns>Decompressed byte array</returns>
        public static byte[] DecompressLayer(byte[] input)
        {
            return input;
            /*using (MemoryStream ms = new MemoryStream(input))
            {
                using (MemoryStream output = new MemoryStream())
                {
                    using (DeflateStream dstream = new DeflateStream(ms, CompressionMode.Decompress))
                    {
                        dstream.CopyTo(output);
                    }
                    return output.ToArray();
                }
            }*/
        }
        #endregion

        #region Methods

        /// <summary>
        /// Rebuild layer properties based on slice settings
        /// </summary>
        public void RebuildLayersProperties(bool recalculateZPos = true, string property = null)
        {
            //var layerHeight = SlicerFile.LayerHeight;
            for (uint layerIndex = 0; layerIndex < Count; layerIndex++)
            {
                var layer = this[layerIndex];
                layer.Index = layerIndex;

                if (property is null || property == nameof(SlicerFile.BottomLayerCount))
                {
                    layer.ExposureTime = SlicerFile.GetInitialLayerValueOrNormal(layerIndex, SlicerFile.BottomExposureTime, SlicerFile.ExposureTime);
                    layer.LiftHeight = SlicerFile.GetInitialLayerValueOrNormal(layerIndex, SlicerFile.BottomLiftHeight, SlicerFile.LiftHeight);
                    layer.LiftSpeed = SlicerFile.GetInitialLayerValueOrNormal(layerIndex, SlicerFile.BottomLiftSpeed, SlicerFile.LiftSpeed);
                    layer.RetractSpeed = SlicerFile.RetractSpeed;
                    layer.LightPWM = SlicerFile.GetInitialLayerValueOrNormal(layerIndex, SlicerFile.BottomLightPWM, SlicerFile.LightPWM);
                    layer.LightOffDelay = SlicerFile.GetInitialLayerValueOrNormal(layerIndex, SlicerFile.BottomLightOffDelay, SlicerFile.LightOffDelay);
                }
                else
                {
                    if (layer.IsNormalLayer)
                    {
                        switch (property)
                        {
                            case nameof(SlicerFile.ExposureTime):
                                layer.ExposureTime = SlicerFile.ExposureTime;
                                break;
                            case nameof(SlicerFile.LiftHeight):
                                layer.LiftHeight = SlicerFile.LiftHeight;
                                break;
                            case nameof(SlicerFile.LiftSpeed):
                                layer.LiftSpeed = SlicerFile.LiftSpeed;
                                break;
                            case nameof(SlicerFile.LightOffDelay):
                                layer.LightOffDelay = SlicerFile.LightOffDelay;
                                break;
                            case nameof(SlicerFile.LightPWM):
                                layer.LightPWM = SlicerFile.LightPWM;
                                break;
                        }
                    }
                    else // Bottom layers
                    {
                        switch (property)
                        {
                            case nameof(SlicerFile.BottomExposureTime):
                                layer.ExposureTime = SlicerFile.BottomExposureTime;
                                break;
                            case nameof(SlicerFile.BottomLiftHeight):
                                layer.LiftHeight = SlicerFile.BottomLiftHeight;
                                break;
                            case nameof(SlicerFile.BottomLiftSpeed):
                                layer.LiftSpeed = SlicerFile.BottomLiftSpeed;
                                break;
                            case nameof(SlicerFile.RetractSpeed):
                                layer.RetractSpeed = SlicerFile.RetractSpeed;
                                break;
                            case nameof(SlicerFile.BottomLightOffDelay):
                                layer.LightOffDelay = SlicerFile.BottomLightOffDelay;
                                break;
                            case nameof(SlicerFile.BottomLightPWM):
                                layer.LightPWM = SlicerFile.BottomLightPWM;
                                break;
                        }
                    }
                }

                if (recalculateZPos)
                {
                    layer.PositionZ = SlicerFile.GetHeightFromLayer(layerIndex);
                }
            }

            SlicerFile?.RebuildGCode();
        }

        /// <summary>
        /// Set LiftHeight to 0 if previous and current have same PositionZ
        /// </summary>
        public void SetNoLiftForSamePositionedLayers()
        {
            for (int layerIndex = 1; layerIndex < Count; layerIndex++)
            {
                if (this[layerIndex - 1].PositionZ != this[layerIndex].PositionZ) continue;
                this[layerIndex].LiftHeight = 0;
            }
            SlicerFile?.RebuildGCode();
        }

        public Rectangle GetBoundingRectangle(OperationProgress progress = null)
        {
            if (!_boundingRectangle.IsEmpty || Count == 0 || this[0] is null) return _boundingRectangle;
            progress ??= new OperationProgress(OperationProgress.StatusOptimizingBounds, Count - 1);
            _boundingRectangle = this[0].BoundingRectangle;
            if (_boundingRectangle.IsEmpty) // Safe checking
            {
                progress.Reset(OperationProgress.StatusOptimizingBounds, Count-1);
                Parallel.For(0, Count, layerIndex =>
                {
                    if (progress.Token.IsCancellationRequested) return;
                    
                    this[layerIndex].GetBoundingRectangle();

                    if (progress is null) return;
                    lock (progress.Mutex)
                    {
                        progress++;
                    }
                });
                _boundingRectangle = this[0].BoundingRectangle;

                if (progress is not null && progress.Token.IsCancellationRequested)
                {
                    _boundingRectangle = Rectangle.Empty;
                    progress.Token.ThrowIfCancellationRequested();
                }
            }

            progress.Reset(OperationProgress.StatusCalculatingBounds, Count-1);
            for (int i = 1; i < Count; i++)
            {
                if(this[i].BoundingRectangle.IsEmpty) continue;
                _boundingRectangle = Rectangle.Union(_boundingRectangle, this[i].BoundingRectangle);
                progress++;
            }
            RaisePropertyChanged(nameof(BoundingRectangle));
            return _boundingRectangle;
        }

        /// <summary>
        /// Add a layer
        /// </summary>
        /// <param name="index">Layer index</param>
        /// <param name="layer">Layer to add</param>
        /// <param name="makeClone">True to add a clone of the layer</param>
        public void AddLayer(uint index, Layer layer, bool makeClone = false)
        {
            if (Layers[index] is not null && layer is not null) layer.IsModified = true;
            Layers[index] = makeClone && layer is not null ? layer.Clone() : layer;
            if (layer is not null)
            {
                layer.ParentLayerManager = this;
            }
        }

        /// <summary>
        /// Add a list of layers
        /// </summary>
        /// <param name="layers">Layers to add</param>
        /// <param name="makeClone">True to add a clone of layers</param>
        public void AddLayers(IEnumerable<Layer> layers, bool makeClone = false)
        {
            //layer.Index = index;
            foreach (var layer in layers)
            {
                layer.ParentLayerManager = this;
                Layers[layer.Index] = makeClone ? layer.Clone() : layer;
            }
        }

        /// <summary>
        /// Get layer given index
        /// </summary>
        /// <param name="index">Layer index</param>
        /// <returns></returns>
        public Layer GetLayer(uint index)
        {
            return Layers[index];
        }

        public static void MutateGetVarsIterationChamfer(uint startLayerIndex, uint endLayerIndex, int iterationsStart, int iterationsEnd, ref bool isFade, out float iterationSteps, out int maxIteration)
        {
            iterationSteps = 0;
            maxIteration = 0;
            isFade = isFade && startLayerIndex != endLayerIndex && iterationsStart != iterationsEnd;
            if (!isFade) return;
            iterationSteps = Math.Abs((iterationsStart - (float)iterationsEnd) / ((float)endLayerIndex - startLayerIndex));
            maxIteration = Math.Max(iterationsStart, iterationsEnd);
        }

        public static int MutateGetIterationVar(bool isFade, int iterationsStart, int iterationsEnd, float iterationSteps, int maxIteration, uint startLayerIndex, uint layerIndex)
        {
            if (!isFade) return iterationsStart;
            // calculate iterations based on range
            int iterations = (int)(iterationsStart < iterationsEnd
                ? iterationsStart + (layerIndex - startLayerIndex) * iterationSteps
                : iterationsStart - (layerIndex - startLayerIndex) * iterationSteps);

            // constrain
            return Math.Min(Math.Max(0, iterations), maxIteration);
        }

        public static int MutateGetIterationChamfer(uint layerIndex, uint startLayerIndex, uint endLayerIndex, int iterationsStart,
            int iterationsEnd, bool isFade)
        {
            MutateGetVarsIterationChamfer(startLayerIndex, endLayerIndex, iterationsStart, iterationsEnd, ref isFade,
                out float iterationSteps, out int maxIteration);
            return MutateGetIterationVar(isFade, iterationsStart, iterationsEnd, iterationSteps, maxIteration, startLayerIndex, layerIndex);
        }

        public unsafe List<LayerIssue> GetAllIssuesBeta(
            IslandDetectionConfiguration islandConfig = null,
            OverhangDetectionConfiguration overhangConfig = null,
            ResinTrapDetectionConfiguration resinTrapConfig = null,
            TouchingBoundDetectionConfiguration touchBoundConfig = null,
            bool emptyLayersConfig = true,
            List<LayerIssue> ignoredIssues = null,
            OperationProgress progress = null)
        {
            islandConfig ??= new IslandDetectionConfiguration();
            overhangConfig ??= new OverhangDetectionConfiguration();
            resinTrapConfig ??= new ResinTrapDetectionConfiguration();
            touchBoundConfig ??= new TouchingBoundDetectionConfiguration();
            progress ??= new OperationProgress();

            var result = new ConcurrentBag<LayerIssue>();
            if (!islandConfig.Enabled && !overhangConfig.Enabled && !resinTrapConfig.Enabled && !touchBoundConfig.Enabled && !emptyLayersConfig) return result.ToList();

            ConcurrentDictionary<uint, List<LayerHollowArea>> layerHollowAreas = new();
            List<uint> actionLayers = new();
            bool checkedEmptyLayers = false;
            Mat emptyMat = new ();
            Mat[] cachedLayers = new Mat[Count];
            const uint cacheCount = 300;

            bool IsIgnored(LayerIssue issue) => !(ignoredIssues is null) && ignoredIssues.Count > 0 && ignoredIssues.Contains(issue);
            bool AddIssue(LayerIssue issue)
            {
                if (IsIgnored(issue)) return false;
                result.Add(issue);
                return true;
            }

            Mat GetCachedMat(uint layerIndex)
            {
                if (cachedLayers[layerIndex] is not null) return cachedLayers[layerIndex];
                Parallel.For(layerIndex, Math.Min(layerIndex + cacheCount, Count), i =>
                {
                    if (this[i].IsEmpty) return; // empty layers
                    cachedLayers[i] = this[i].LayerMat;
                });
                return cachedLayers[layerIndex];
            }


            if (touchBoundConfig.Enabled || resinTrapConfig.Enabled ||
                (islandConfig.Enabled && islandConfig.WhiteListLayers is null) ||
                (overhangConfig.Enabled && overhangConfig.WhiteListLayers is null)
            )
            {
                checkedEmptyLayers = true;
                for (uint layerIndex = 0; layerIndex < Count; layerIndex++)
                {
                    var layer = this[layerIndex];
                    if (layer.IsEmpty)
                    {
                        cachedLayers[layerIndex] = emptyMat;
                        if (emptyLayersConfig)
                        {
                            AddIssue(new LayerIssue(layer, LayerIssue.IssueType.EmptyLayer));
                        }
                        continue;
                    }
                    actionLayers.Add(layerIndex);
                }
            }

            if (!checkedEmptyLayers && emptyLayersConfig)
            {
                checkedEmptyLayers = true;
                for (uint layerIndex = 0; layerIndex < Count; layerIndex++)
                {
                    var layer = this[layerIndex];
                    if (layer.IsEmpty)
                    {
                        AddIssue(new LayerIssue(layer, LayerIssue.IssueType.EmptyLayer));
                    }
                }
            }

            for (var i = 0; i < actionLayers.Count; i++)
            {
                var rootLayerIndex = actionLayers[i];
                GetCachedMat(rootLayerIndex);
                progress.Token.ThrowIfCancellationRequested();
                uint layerAdvance = (uint) Math.Min(i + cacheCount, actionLayers.Count);
                Parallel.For(i, layerAdvance, l =>
                {
                    var layerIndex = actionLayers[(int) l];
                    var layer = this[layerIndex];
                    if (layer.IsEmpty)
                    {
                        lock (progress.Mutex)
                        {
                            progress++;
                        }
                        
                        return; // Empty layer
                    }

                    var image = cachedLayers[layerIndex];

                    int step = image.Step;
                    var span = image.GetBytePointer();

                    if (touchBoundConfig.Enabled)
                    {
                        // TouchingBounds Checker
                        List<Point> pixels = new List<Point>();
                        bool touchTop = layer.BoundingRectangle.Top <= touchBoundConfig.MarginTop;
                        bool touchBottom = layer.BoundingRectangle.Bottom >= image.Height - touchBoundConfig.MarginBottom;
                        bool touchLeft = layer.BoundingRectangle.Left <= touchBoundConfig.MarginLeft;
                        bool touchRight = layer.BoundingRectangle.Right >= image.Width - touchBoundConfig.MarginRight;
                        if (touchTop || touchBottom)
                        {
                            for (int x = 0; x < image.Width; x++) // Check Top and Bottom bounds
                            {
                                if (touchTop)
                                {
                                    for (int y = 0; y < touchBoundConfig.MarginTop; y++) // Top
                                    {
                                        if (span[image.GetPixelPos(x, y)] >=
                                            touchBoundConfig.MinimumPixelBrightness)
                                        {
                                            pixels.Add(new Point(x, y));
                                        }
                                    }
                                }

                                if (touchBottom)
                                {
                                    for (int y = image.Height - touchBoundConfig.MarginBottom;
                                        y < image.Height;
                                        y++) // Bottom
                                    {
                                        if (span[image.GetPixelPos(x, y)] >=
                                            touchBoundConfig.MinimumPixelBrightness)
                                        {
                                            pixels.Add(new Point(x, y));
                                        }
                                    }
                                }
                            }
                        }

                        if (touchLeft || touchRight)
                        {
                            for (int y = touchBoundConfig.MarginTop;
                                y < image.Height - touchBoundConfig.MarginBottom;
                                y++) // Check Left and Right bounds
                            {
                                if (touchLeft)
                                {
                                    for (int x = 0; x < touchBoundConfig.MarginLeft; x++) // Left
                                    {
                                        if (span[image.GetPixelPos(x, y)] >=
                                            touchBoundConfig.MinimumPixelBrightness)
                                        {
                                            pixels.Add(new Point(x, y));
                                        }
                                    }
                                }

                                if (touchRight)
                                {
                                    for (int x = image.Width - touchBoundConfig.MarginRight; x < image.Width; x++) // Right
                                    {
                                        if (span[image.GetPixelPos(x, y)] >=
                                            touchBoundConfig.MinimumPixelBrightness)
                                        {
                                            pixels.Add(new Point(x, y));
                                        }
                                    }
                                }
                            }
                        }

                        if (pixels.Count > 0)
                        {
                            AddIssue(new LayerIssue(layer, LayerIssue.IssueType.TouchingBound,
                                pixels.ToArray()));
                        }
                    }

                    lock (progress.Mutex)
                    {
                        progress++;
                    }
                });

                for (; i < layerAdvance - 1; i++)
                {
                    cachedLayers[actionLayers[i]].Dispose();
                    cachedLayers[actionLayers[i]] = null;
                }


                // Wait for jobs
                    /*foreach (var task in taskList)
                    {
                        task.Wait();
                    }

                    if (layerIndex > 0)
                    {
                        cachedLayers[layerIndex - 1].Dispose();
                        cachedLayers[layerIndex - 1] = null;
                    }*/
            }

            return result.ToList();
        }

        public List<LayerIssue> GetAllIssues(
            IslandDetectionConfiguration islandConfig = null,
            OverhangDetectionConfiguration overhangConfig = null, 
            ResinTrapDetectionConfiguration resinTrapConfig = null,
            TouchingBoundDetectionConfiguration touchBoundConfig = null,
            bool emptyLayersConfig = true,
            List<LayerIssue> ignoredIssues = null,
            OperationProgress progress = null)
        {
            
            islandConfig ??= new IslandDetectionConfiguration();
            overhangConfig ??= new OverhangDetectionConfiguration();
            resinTrapConfig ??= new ResinTrapDetectionConfiguration();
            touchBoundConfig ??= new TouchingBoundDetectionConfiguration();
            progress ??= new OperationProgress();
            
            var result = new ConcurrentBag<LayerIssue>();
            var layerHollowAreas = new ConcurrentDictionary<uint, List<LayerHollowArea>>();

            bool IsIgnored(LayerIssue issue) => !(ignoredIssues is null) && ignoredIssues.Count > 0 && ignoredIssues.Contains(issue);

            bool AddIssue(LayerIssue issue)
            {
                if (IsIgnored(issue)) return false;
                result.Add(issue);
                return true;
            }

            if (islandConfig.Enabled || overhangConfig.Enabled || resinTrapConfig.Enabled || touchBoundConfig.Enabled || emptyLayersConfig)
            {
                progress.Reset(OperationProgress.StatusIslands, Count);

                // Detect contours
                Parallel.ForEach(this,
                //new ParallelOptions{MaxDegreeOfParallelism = 1},
                layer =>
                {
                    if (progress.Token.IsCancellationRequested) return;
                    if (layer.IsEmpty)
                    {
                        if (emptyLayersConfig)
                        {
                            AddIssue(new LayerIssue(layer, LayerIssue.IssueType.EmptyLayer));
                        }

                        progress.LockAndIncrement();

                        return;
                    }

                    // Spare a decoding cycle
                    if (!touchBoundConfig.Enabled &&
                        !resinTrapConfig.Enabled &&
                        !overhangConfig.Enabled || overhangConfig.Enabled && (layer.Index == 0 || overhangConfig.WhiteListLayers is not null && !overhangConfig.WhiteListLayers.Contains(layer.Index)) &&
                        !islandConfig.Enabled || islandConfig.Enabled && (layer.Index == 0 || islandConfig.WhiteListLayers is not null && !islandConfig.WhiteListLayers.Contains(layer.Index))
                        )
                    {
                        progress.LockAndIncrement();
                        
                        return;
                    }

                    using (var image = layer.LayerMat)
                    {
                        int step = image.Step;
                        var span = image.GetPixelSpan<byte>();

                        if (touchBoundConfig.Enabled)
                        {
                            // TouchingBounds Checker
                            List<Point> pixels = new ();
                            bool touchTop = layer.BoundingRectangle.Top <= touchBoundConfig.MarginTop;
                            bool touchBottom = layer.BoundingRectangle.Bottom >=
                                               image.Height - touchBoundConfig.MarginBottom;
                            bool touchLeft = layer.BoundingRectangle.Left <= touchBoundConfig.MarginLeft;
                            bool touchRight = layer.BoundingRectangle.Right >=
                                              image.Width - touchBoundConfig.MarginRight;
                            if (touchTop || touchBottom)
                            {
                                for (int x = 0; x < image.Width; x++) // Check Top and Bottom bounds
                                {
                                    if (touchTop)
                                    {
                                        for (int y = 0; y < touchBoundConfig.MarginTop; y++) // Top
                                        {
                                            if (span[image.GetPixelPos(x, y)] >=
                                                touchBoundConfig.MinimumPixelBrightness)
                                            {
                                                pixels.Add(new Point(x, y));
                                            }
                                        }
                                    }

                                    if (touchBottom)
                                    {
                                        for (int y = image.Height - touchBoundConfig.MarginBottom;
                                            y < image.Height;
                                            y++) // Bottom
                                        {
                                            if (span[image.GetPixelPos(x, y)] >=
                                                touchBoundConfig.MinimumPixelBrightness)
                                            {
                                                pixels.Add(new Point(x, y));
                                            }
                                        }
                                    }

                                }
                            }

                            if (touchLeft || touchRight)
                            {
                                for (int y = touchBoundConfig.MarginTop;
                                    y < image.Height - touchBoundConfig.MarginBottom;
                                    y++) // Check Left and Right bounds
                                {
                                    if (touchLeft)
                                    {
                                        for (int x = 0; x < touchBoundConfig.MarginLeft; x++) // Left
                                        {
                                            if (span[image.GetPixelPos(x, y)] >=
                                                touchBoundConfig.MinimumPixelBrightness)
                                            {
                                                pixels.Add(new Point(x, y));
                                            }
                                        }
                                    }

                                    if (touchRight)
                                    {
                                        for (int x = image.Width - touchBoundConfig.MarginRight;
                                            x < image.Width;
                                            x++) // Right
                                        {
                                            if (span[image.GetPixelPos(x, y)] >=
                                                touchBoundConfig.MinimumPixelBrightness)
                                            {
                                                pixels.Add(new Point(x, y));
                                            }
                                        }
                                    }
                                }
                            }

                            if (pixels.Count > 0)
                            {
                                AddIssue(new LayerIssue(layer, LayerIssue.IssueType.TouchingBound,
                                    pixels.ToArray()));
                            }
                        }

                        if (layer.Index > 0) // No islands nor overhangs for layer 0
                        {
                            Mat previousImage = null;
                            Span<byte> previousSpan = null;

                            if (islandConfig.Enabled)
                            {
                                bool canProcessCheck = true;
                                if (islandConfig.WhiteListLayers is not null) // Check white list
                                {
                                    if (!islandConfig.WhiteListLayers.Contains(layer.Index))
                                    {
                                        canProcessCheck = false;
                                    }
                                }

                                if (canProcessCheck)
                                {
                                    bool needDispose = false;
                                    Mat islandImage;
                                    if (islandConfig.BinaryThreshold > 0)
                                    {
                                        needDispose = true;
                                        islandImage = new();
                                        CvInvoke.Threshold(image, islandImage, islandConfig.BinaryThreshold, byte.MaxValue,
                                            ThresholdType.Binary);
                                    }
                                    else
                                    {
                                        islandImage = image;
                                    }

                                    using (Mat labels = new Mat())
                                    using (Mat stats = new Mat())
                                    using (Mat centroids = new Mat())
                                    {
                                        var numLabels = CvInvoke.ConnectedComponentsWithStats(islandImage, labels, stats,
                                            centroids,
                                            islandConfig.AllowDiagonalBonds
                                                ? LineType.EightConnected
                                                : LineType.FourConnected);

                                        if(needDispose)
                                        {
                                            islandImage.Dispose();
                                        }

                                        // Get array that contains details of each connected component
                                        var ccStats = stats.GetData();
                                        //stats[i][0]: Left Edge of Connected Component
                                        //stats[i][1]: Top Edge of Connected Component 
                                        //stats[i][2]: Width of Connected Component
                                        //stats[i][3]: Height of Connected Component
                                        //stats[i][4]: Total Area (in pixels) in Connected Component

                                        Span<int> labelSpan = labels.GetPixelSpan<int>();

                                        for (int i = 1; i < numLabels; i++)
                                        {
                                            Rectangle rect = new Rectangle(
                                                (int) ccStats.GetValue(i, (int) ConnectedComponentsTypes.Left),
                                                (int) ccStats.GetValue(i, (int) ConnectedComponentsTypes.Top),
                                                (int) ccStats.GetValue(i, (int) ConnectedComponentsTypes.Width),
                                                (int) ccStats.GetValue(i, (int) ConnectedComponentsTypes.Height));

                                            if (rect.Area() < islandConfig.RequiredAreaToProcessCheck)
                                                continue;

                                            if (previousImage is null)
                                            {
                                                previousImage = this[layer.Index - 1].LayerMat;
                                                previousSpan = previousImage.GetPixelSpan<byte>();
                                            }

                                            List<Point> points = new List<Point>();
                                            uint pixelsSupportingIsland = 0;

                                            for (int y = rect.Y; y < rect.Bottom; y++)
                                            for (int x = rect.X; x < rect.Right; x++)
                                            {
                                                int pixel = step * y + x;
                                                if (
                                                    labelSpan[pixel] !=
                                                    i || // Background pixel or a pixel from another component within the bounding rectangle
                                                    span[pixel] <
                                                    islandConfig
                                                        .RequiredPixelBrightnessToProcessCheck // Low brightness, ignore
                                                ) continue;

                                                points.Add(new Point(x, y));

                                                if (previousSpan[pixel] >=
                                                    islandConfig.RequiredPixelBrightnessToSupport)
                                                {
                                                    pixelsSupportingIsland++;
                                                }
                                            }

                                            if (points.Count == 0) continue; // Should never happen

                                            var requiredSupportingPixels = Math.Max(1,
                                                points.Count * islandConfig.RequiredPixelsToSupportMultiplier);

                                            /*if (pixelsSupportingIsland >= islandConfig.RequiredPixelsToSupport)
                                                isIsland = false; // Not a island, bounding is strong, i think...
                                            else if (pixelsSupportingIsland > 0 &&
                                                points.Count < islandConfig.RequiredPixelsToSupport &&
                                                pixelsSupportingIsland >= Math.Max(1, points.Count / 2))
                                                isIsland = false; // Not a island, but maybe weak bounding...*/

                                            LayerIssue island = null;
                                            if (pixelsSupportingIsland < requiredSupportingPixels)
                                            {
                                                island = new LayerIssue(layer, LayerIssue.IssueType.Island,
                                                    points.ToArray(),
                                                    rect);
                                                /*AddIssue(new LayerIssue(layer, LayerIssue.IssueType.Island,
                                                    points.ToArray(),
                                                    rect));*/
                                            }

                                            // Check for overhangs
                                            if (overhangConfig.Enabled && !overhangConfig.IndependentFromIslands &&
                                                island is null
                                                || island is not null && islandConfig.EnhancedDetection &&
                                                pixelsSupportingIsland >= 10
                                            )
                                            {
                                                points.Clear();
                                                using (var imageRoi = new Mat(image, rect))
                                                using (var previousImageRoi = new Mat(previousImage, rect))
                                                using (var subtractedImage = new Mat())
                                                {
                                                    var anchor = new Point(-1, -1);
                                                    CvInvoke.Subtract(imageRoi, previousImageRoi, subtractedImage);
                                                    CvInvoke.Threshold(subtractedImage, subtractedImage, 127, 255,
                                                        ThresholdType.Binary);

                                                    CvInvoke.Erode(subtractedImage, subtractedImage,
                                                        CvInvoke.GetStructuringElement(ElementShape.Rectangle,
                                                            new Size(3, 3), anchor),
                                                        anchor, overhangConfig.ErodeIterations, BorderType.Default,
                                                        new MCvScalar());

                                                    var subtractedSpan = subtractedImage.GetPixelSpan<byte>();

                                                    for (int y = 0; y < subtractedImage.Height; y++)
                                                    for (int x = 0; x < subtractedImage.Step; x++)
                                                    {
                                                        int labelX = rect.X + x;
                                                        int labelY = rect.Y + y;
                                                        int pixel = subtractedImage.GetPixelPos(x, y);
                                                        int pixelLabel = labelY * step + labelX;
                                                        if (labelSpan[pixelLabel] != i || subtractedSpan[pixel] == 0)
                                                            continue;

                                                        points.Add(new Point(labelX, labelY));
                                                    }

                                                    if (points.Count >= overhangConfig.RequiredPixelsToConsider
                                                    ) // Overhang
                                                    {
                                                        AddIssue(new LayerIssue(
                                                            layer, LayerIssue.IssueType.Overhang, points.ToArray(), rect
                                                        ));
                                                    }
                                                    else if (islandConfig.EnhancedDetection) // No overhang
                                                    {
                                                        island = null;
                                                    }
                                                }
                                            }

                                            if (island is not null)
                                                AddIssue(island);
                                        }
                                    }
                                }
                            }

                            // Overhangs
                            if (!islandConfig.Enabled && overhangConfig.Enabled ||
                                (islandConfig.Enabled && overhangConfig.Enabled &&
                                 overhangConfig.IndependentFromIslands))
                            {
                                bool canProcessCheck = true;
                                if (overhangConfig.WhiteListLayers is not null) // Check white list
                                {
                                    if (!overhangConfig.WhiteListLayers.Contains(layer.Index))
                                    {
                                        canProcessCheck = false;
                                    }
                                }

                                if (canProcessCheck)
                                {
                                    if (previousImage is null)
                                    {
                                        previousImage = this[layer.Index - 1].LayerMat;
                                    }

                                    using (var subtractedImage = new Mat())
                                    using (var vecPoints = new VectorOfPoint())
                                    {
                                        var anchor = new Point(-1, -1);


                                        CvInvoke.Subtract(image, previousImage, subtractedImage);
                                        CvInvoke.Threshold(subtractedImage, subtractedImage, 127, 255,
                                            ThresholdType.Binary);

                                        CvInvoke.Erode(subtractedImage, subtractedImage,
                                            CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(3, 3),
                                                anchor),
                                            anchor, overhangConfig.ErodeIterations, BorderType.Default,
                                            new MCvScalar());

                                        CvInvoke.FindNonZero(subtractedImage, vecPoints);
                                        if (vecPoints.Size >= overhangConfig.RequiredPixelsToConsider)
                                        {
                                            AddIssue(new LayerIssue(
                                                layer, LayerIssue.IssueType.Overhang, vecPoints.ToArray(),
                                                layer.BoundingRectangle
                                            ));
                                        }
                                    }
                                }
                            }

                            previousImage?.Dispose();
                        }

                        if (resinTrapConfig.Enabled)
                        {
                            bool needDispose = false;
                            Mat resinTrapImage;
                            if (resinTrapConfig.BinaryThreshold > 0)
                            {
                                resinTrapImage = new Mat();
                                CvInvoke.Threshold(image, resinTrapImage, resinTrapConfig.BinaryThreshold, byte.MaxValue, ThresholdType.Binary);
                            }
                            else
                            {
                                needDispose = true;
                                resinTrapImage = image;
                            }

                            var listHollowArea = new List<LayerHollowArea>();

                            using VectorOfVectorOfPoint contours = new();
                            using Mat hierarchy = new();
                            CvInvoke.FindContours(resinTrapImage, contours, hierarchy, RetrType.Ccomp, ChainApproxMethod.ChainApproxSimple);

                            if(needDispose)
                            {
                                resinTrapImage.Dispose();
                            }

                            var arr = hierarchy.GetData();
                            //
                            //hierarchy[i][0]: the index of the next contour of the same level
                            //hierarchy[i][1]: the index of the previous contour of the same level
                            //hierarchy[i][2]: the index of the first child
                            //hierarchy[i][3]: the index of the parent
                            //

                            for (int i = 0; i < contours.Size; i++)
                            {
                                if ((int)arr.GetValue(0, i, 2) != -1 || (int)arr.GetValue(0, i, 3) == -1)
                                    continue;

                                var rect = CvInvoke.BoundingRectangle(contours[i]);
                                if (rect.Area() < resinTrapConfig.RequiredAreaToProcessCheck) continue;

                                listHollowArea.Add(new LayerHollowArea(contours[i].ToArray(),
                                    rect,
                                    layer.Index == 0 ||
                                    layer.Index == Count - 1 // First and Last layers, always drains
                                        ? LayerHollowArea.AreaType.Drain
                                        : LayerHollowArea.AreaType.Unknown));

                                if (listHollowArea.Count > 0)
                                    layerHollowAreas.TryAdd(layer.Index, listHollowArea);
                            }
                        }

                    }

                    progress.LockAndIncrement();
                }); // Parallel end
            }

            if (resinTrapConfig.Enabled)
            {
                progress.Reset(OperationProgress.StatusResinTraps, Count);

                for (uint layerIndex = 1; layerIndex < Count - 1; layerIndex++) // First and Last layers, always drains
                {
                    if (progress.Token.IsCancellationRequested) break;
                    if (!layerHollowAreas.TryGetValue(layerIndex, out var areas))
                        continue; // No hollow areas in this layer, ignore

                    byte areaCount = 0;
                    //foreach (var area in areas)

                    //Parallel.ForEach(from t in areas where t.Type == LayerHollowArea.AreaType.Unknown select t, 
                    //    new ParallelOptions{MaxDegreeOfParallelism = 1}, area =>
                    foreach(var area in areas)
                    {
                        //if (progress.Token.IsCancellationRequested) return;
                        if (area.Type != LayerHollowArea.AreaType.Unknown) continue; // processed, ignore
                        progress.Token.ThrowIfCancellationRequested();
                        area.Type = LayerHollowArea.AreaType.Trap;

                        areaCount++;

                        List<LayerHollowArea> linkedAreas = new();

                        for (sbyte dir = 1; dir >= -1 && area.Type != LayerHollowArea.AreaType.Drain; dir -= 2)
                            //Parallel.ForEach(new sbyte[] {1, -1}, new ParallelOptions {MaxDegreeOfParallelism = 2}, dir =>
                        {
                            Queue<LayerHollowArea> queue = new();
                            queue.Enqueue(area);
                            area.Processed = false;
                            int nextLayerIndex = (int) layerIndex;
                            while (queue.Count > 0 && area.Type != LayerHollowArea.AreaType.Drain)
                            {
                                //if (progress.Token.IsCancellationRequested) return;
                                progress.Token.ThrowIfCancellationRequested();

                                LayerHollowArea checkArea = queue.Dequeue();
                                if (checkArea.Processed) continue;
                                checkArea.Processed = true;
                                nextLayerIndex += dir;

                                if (nextLayerIndex < 0 || nextLayerIndex >= Count)
                                    break; // Exhausted layers
                                bool haveNextAreas =
                                    layerHollowAreas.TryGetValue((uint) nextLayerIndex, out var nextAreas);
                                Dictionary<int, LayerHollowArea> intersectingAreas = new();

                                progress.Reset(OperationProgress.StatusResinTraps, Count, (uint) nextLayerIndex);

                                using (var image = this[nextLayerIndex].LayerMat)
                                {
                                    var span = image.GetPixelSpan<byte>();
                                    using (var emguImage = image.CloneBlank())
                                    {
                                        using (var vec =
                                            new VectorOfVectorOfPoint(new VectorOfPoint(checkArea.Contour)))
                                        {
                                            CvInvoke.DrawContours(emguImage, vec, -1, EmguExtensions.WhiteByte, -1);
                                        }

                                        using (var intersectingAreasMat = image.CloneBlank())
                                        {
                                            if (haveNextAreas)
                                            {
                                                foreach (var nextArea in nextAreas)
                                                {
                                                    if (!checkArea.BoundingRectangle.IntersectsWith(
                                                        nextArea.BoundingRectangle)) continue;
                                                    intersectingAreas.Add(intersectingAreas.Count + 1, nextArea);
                                                    using var vec = new VectorOfVectorOfPoint(new VectorOfPoint(nextArea.Contour));
                                                    CvInvoke.DrawContours(intersectingAreasMat, vec, -1,
                                                        new MCvScalar(intersectingAreas.Count), -1);
                                                }
                                            }

                                            //Debug.WriteLine($"Area Count: {areaCount} | Next Areas: {intersectingAreas.Count} | Layer: {layerIndex} | Next Layer: {nextLayerIndex} | Dir: {dir}");

                                            bool exitPixelLoop = false;
                                            uint blackCount = 0;

                                            var spanContour = emguImage.GetPixelSpan<byte>();
                                            var spanIntersect = intersectingAreasMat.GetPixelSpan<byte>();
                                            for (int y = checkArea.BoundingRectangle.Y;
                                                y < checkArea.BoundingRectangle.Bottom &&
                                                area.Type != LayerHollowArea.AreaType.Drain && !exitPixelLoop;
                                                y++)
                                            {
                                                int pixelPos = image.GetPixelPos(checkArea.BoundingRectangle.X, y) - 1;
                                                for (int x = checkArea.BoundingRectangle.X;
                                                    x < checkArea.BoundingRectangle.Right &&
                                                    area.Type != LayerHollowArea.AreaType.Drain && !exitPixelLoop;
                                                    x++)
                                                {
                                                    pixelPos++;

                                                    if (spanContour[pixelPos] != 255) continue; // No contour
                                                    if (span[pixelPos] > resinTrapConfig.MaximumPixelBrightnessToDrain)
                                                        continue; // Threshold to ignore white area
                                                    blackCount++;

                                                    if (intersectingAreas.Count > 0) // Have areas, can be on same area path or not
                                                    {
                                                        byte i = spanIntersect[pixelPos];
                                                        if (i == 0 || !intersectingAreas.ContainsKey(i)) // Black pixels
                                                            continue;

                                                        //Debug.WriteLine($"BlackCount: {blackCount}, pixel color: {i}, layerindex: {layerIndex}");

                                                        if (intersectingAreas[i].Type == LayerHollowArea.AreaType.Drain) // Found a drain, stop query
                                                        {
                                                            area.Type = LayerHollowArea.AreaType.Drain;
                                                            exitPixelLoop = true;
                                                        }
                                                        else
                                                        {
                                                            queue.Enqueue(intersectingAreas[i]);
                                                        }

                                                        linkedAreas.Add(intersectingAreas[i]);
                                                        intersectingAreas.Remove(i);
                                                        if (intersectingAreas.Count == 0
                                                        ) // Intersection areas sweep end, quit this path
                                                        {
                                                            exitPixelLoop = true;
                                                            break;
                                                        }

                                                        //break;

                                                        // Old Way

                                                        /*foreach (var nextAreaCheck in intersectingAreas)
                                                        {
                                                            using (var vec = new VectorOfPoint(nextAreaCheck.Value.Contour))
                                                            {
                                                                //Debug.WriteLine(CvInvoke.PointPolygonTest(vec, new PointF(x, y), false));
                                                                if (CvInvoke.PointPolygonTest(vec, new PointF(x, y), false) < 0) continue;
                                                            }
    
                                                            if (nextAreaCheck.Value.Type == LayerHollowArea.AreaType.Drain) // Found a drain, stop query
                                                            {
                                                                area.Type = LayerHollowArea.AreaType.Drain;
                                                                exitPixelLoop = true;
                                                            }
                                                            else
                                                            {
                                                                queue.Enqueue(nextAreaCheck.Value);
                                                            }
    
                                                            linkedAreas.Add(nextAreaCheck.Value);
                                                            intersectingAreas.Remove(nextAreaCheck.Key);
                                                            if (intersectingAreas.Count == 0)
                                                            {
                                                                haveNextAreas = false;
                                                                exitPixelLoop = true;
                                                            }
                                                            //exitPixelLoop = true;
                                                            break;
    
                                                        }*/
                                                    }
                                                    else if (blackCount > Math.Min(checkArea.Contour.Length / 2,
                                                        resinTrapConfig.RequiredBlackPixelsToDrain)
                                                    ) // Black pixel without next areas = Drain
                                                    {
                                                        area.Type = LayerHollowArea.AreaType.Drain;
                                                        exitPixelLoop = true;
                                                        break;
                                                    }
                                                } // X loop
                                            } // Y loop

                                            if (queue.Count == 0 && blackCount > Math.Min(checkArea.Contour.Length / 2,
                                                resinTrapConfig.RequiredBlackPixelsToDrain))
                                            {
                                                area.Type = LayerHollowArea.AreaType.Drain;
                                            }

                                        } // Dispose intersecting image
                                    } // Dispose emgu image
                                } // Dispose image
                            } // Areas loop
                        } // Dir layer loop

                        foreach (var linkedArea in linkedAreas) // Update linked areas
                        {
                            linkedArea.Type = area.Type;
                        }
                    }//);
                }
            }

            /*var resultSorted = result.ToList();
            resultSorted.Sort((issue, layerIssue) =>
            {
                int ret = issue.Type.CompareTo(layerIssue.Type);
                return ret != 0 ? ret : issue.LayerIndex.CompareTo(layerIssue.LayerIndex);
            });*/
            if (progress.Token.IsCancellationRequested) return result.OrderBy(issue => issue.Type).ThenBy(issue => issue.LayerIndex).ThenBy(issue => issue.PixelsCount).ToList();

            for (uint layerIndex = 0; layerIndex < Count; layerIndex++)
            {
                if (!layerHollowAreas.TryGetValue(layerIndex, out var list)) continue;
                if (list.Count == 0) continue;
                foreach (var issue in 
                        from area 
                        in list 
                        where area.Type == LayerHollowArea.AreaType.Trap 
                        select new LayerIssue(this[layerIndex], LayerIssue.IssueType.ResinTrap, area.Contour, area.BoundingRectangle))
                {
                    AddIssue(issue);
                }
            }

            return result.OrderBy(issue => issue.Type).ThenBy(issue => issue.LayerIndex).ThenBy(issue => issue.PixelsCount).ToList();
        }


        public void DrawModifications(IList<PixelOperation> drawings, OperationProgress progress = null)
        {
            progress ??= new OperationProgress();
            progress.Reset("Drawings", (uint) drawings.Count);

            ConcurrentDictionary<uint, Mat> modifiedLayers = new ConcurrentDictionary<uint, Mat>();
            for (var i = 0; i < drawings.Count; i++)
            {
                var operation = drawings[i];
                VectorOfVectorOfPoint layerContours = null;
                Mat layerHierarchy = null;

                if (operation.OperationType == PixelOperation.PixelOperationType.Drawing)
                {
                    var operationDrawing = (PixelDrawing) operation;
                    var mat = modifiedLayers.GetOrAdd(operation.LayerIndex, u => this[operation.LayerIndex].LayerMat);

                    if (operationDrawing.BrushSize == 1)
                    {
                        mat.SetByte(operation.Location.X, operation.Location.Y, operationDrawing.Brightness);
                        continue;
                    }

                    switch (operationDrawing.BrushShape)
                    {
                        case PixelDrawing.BrushShapeType.Rectangle:
                            CvInvoke.Rectangle(mat, operationDrawing.Rectangle, new MCvScalar(operationDrawing.Brightness), operationDrawing.Thickness, operationDrawing.LineType);
                            break;
                        case PixelDrawing.BrushShapeType.Circle:
                            CvInvoke.Circle(mat, operation.Location, operationDrawing.BrushSize / 2,
                                new MCvScalar(operationDrawing.Brightness), operationDrawing.Thickness, operationDrawing.LineType);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                else if (operation.OperationType == PixelOperation.PixelOperationType.Text)
                {
                    var operationText = (PixelText)operation;
                    var mat = modifiedLayers.GetOrAdd(operation.LayerIndex, u => this[operation.LayerIndex].LayerMat);

                    CvInvoke.PutText(mat, operationText.Text, operationText.Location, operationText.Font, operationText.FontScale, new MCvScalar(operationText.Brightness), operationText.Thickness, operationText.LineType, operationText.Mirror);
                }
                else if (operation.OperationType == PixelOperation.PixelOperationType.Eraser)
                {
                    var mat = modifiedLayers.GetOrAdd(operation.LayerIndex, u => this[operation.LayerIndex].LayerMat);

                    if (layerContours is null)
                    {
                        layerContours = new VectorOfVectorOfPoint();
                        layerHierarchy = new Mat();

                        CvInvoke.FindContours(mat, layerContours, layerHierarchy, RetrType.Ccomp,
                            ChainApproxMethod.ChainApproxSimple);
                    }

                    if (mat.GetByte(operation.Location) >= 10)
                    {
                        for (int contourIdx = 0; contourIdx < layerContours.Size; contourIdx++)
                        {
                            if (!(CvInvoke.PointPolygonTest(layerContours[contourIdx], operation.Location, false) >= 0))
                                continue;
                            CvInvoke.DrawContours(mat, layerContours, contourIdx, new MCvScalar(operation.PixelBrightness), -1);
                            break;
                        }
                    }
                }
                else if (operation.OperationType == PixelOperation.PixelOperationType.Supports)
                {
                    var operationSupport = (PixelSupport)operation;
                    int drawnLayers = 0;
                    for (int operationLayer = (int)operation.LayerIndex-1; operationLayer >= 0; operationLayer--)
                    {
                        var mat = modifiedLayers.GetOrAdd((uint) operationLayer, u => this[operationLayer].LayerMat);
                        int radius = (operationLayer > 10 ? Math.Min(operationSupport.TipDiameter + drawnLayers, operationSupport.PillarDiameter) : operationSupport.BaseDiameter) / 2;
                        uint whitePixels;

                        int yStart = Math.Max(0, operation.Location.Y - operationSupport.TipDiameter / 2);
                        int xStart = Math.Max(0, operation.Location.X - operationSupport.TipDiameter / 2);

                        using (Mat matCircleRoi = new Mat(mat, new Rectangle(xStart, yStart, operationSupport.TipDiameter, operationSupport.TipDiameter)))
                        {
                            using (Mat matCircleMask = matCircleRoi.CloneBlank())
                            {
                                CvInvoke.Circle(matCircleMask, new Point(operationSupport.TipDiameter / 2, operationSupport.TipDiameter / 2),
                                    operationSupport.TipDiameter / 2, new MCvScalar(operation.PixelBrightness), -1);
                                CvInvoke.BitwiseAnd(matCircleRoi, matCircleMask, matCircleMask);
                                whitePixels = (uint) CvInvoke.CountNonZero(matCircleMask);
                            }
                        }

                        if (whitePixels >= Math.Pow(operationSupport.TipDiameter, 2) / 3)
                        {
                            //CvInvoke.Circle(mat, operation.Location, radius, new MCvScalar(255), -1);
                            if (drawnLayers == 0) continue; // Supports nonexistent, keep digging
                            break; // White area end supporting
                        }

                        CvInvoke.Circle(mat, operation.Location, radius, new MCvScalar(operation.PixelBrightness), -1, operationSupport.LineType);
                        drawnLayers++;
                    }
                }
                else if (operation.OperationType == PixelOperation.PixelOperationType.DrainHole)
                {
                    uint drawnLayers = 0;
                    var operationDrainHole = (PixelDrainHole)operation;
                    for (int operationLayer = (int)operation.LayerIndex; operationLayer >= 0; operationLayer--)
                    {
                        var mat = modifiedLayers.GetOrAdd((uint)operationLayer, u => this[operationLayer].LayerMat);
                        int radius =  operationDrainHole.Diameter / 2;
                        uint blackPixels;

                        int yStart = Math.Max(0, operation.Location.Y - radius);
                        int xStart = Math.Max(0, operation.Location.X - radius);

                        using (Mat matCircleRoi = new Mat(mat, new Rectangle(xStart, yStart, operationDrainHole.Diameter, operationDrainHole.Diameter)))
                        {
                            using (Mat matCircleRoiInv = new Mat())
                            {
                                CvInvoke.Threshold(matCircleRoi, matCircleRoiInv, 100, 255, ThresholdType.BinaryInv);
                                using (Mat matCircleMask = matCircleRoi.CloneBlank())
                                {
                                    CvInvoke.Circle(matCircleMask, new Point(radius, radius), radius, new MCvScalar(255), -1);
                                    CvInvoke.BitwiseAnd(matCircleRoiInv, matCircleMask, matCircleMask);
                                    blackPixels = (uint) CvInvoke.CountNonZero(matCircleMask);
                                }
                            }
                        }

                        if (blackPixels >= Math.Pow(operationDrainHole.Diameter, 2) / 3) // Enough area to drain?
                        {
                            if (drawnLayers == 0) continue; // Drill not found a target yet, keep digging
                            break; // Stop drill drain found!
                        }
                        
                        CvInvoke.Circle(mat, operation.Location, radius, new MCvScalar(0), -1, operationDrainHole.LineType);
                        drawnLayers++;
                    }
                }

                layerContours?.Dispose();
                layerHierarchy?.Dispose();

                progress++;
            }

            progress.Reset("Saving", (uint) modifiedLayers.Count);
            Parallel.ForEach(modifiedLayers, (modfiedLayer, state) =>
            {
                this[modfiedLayer.Key].LayerMat = modfiedLayer.Value;
                modfiedLayer.Value.Dispose();

                lock (progress)
                {
                    progress++;
                }
            });
            /*foreach (var modfiedLayer in modfiedLayers)
            {
                this[modfiedLayer.Key].LayerMat = modfiedLayer.Value;
                modfiedLayer.Value.Dispose();
                progress++;
            }*/

            //pixelHistory.Clear();
        }

        

        /// <summary>
        /// Desmodify all layers
        /// </summary>
        public void Desmodify()
        {
            for (uint i = 0; i < Count; i++)
            {
                Layers[i].IsModified = false;
            }
        }

        /// <summary>
        /// Reallocate with new size
        /// </summary>
        /// <returns></returns>
        public LayerManager ReallocateNew(uint newLayerCount, bool makeClone = false)
        {
            LayerManager layerManager = new LayerManager(newLayerCount, SlicerFile);
            foreach (var layer in this)
            {
                if (layer.Index >= newLayerCount) break;
                layerManager[layer.Index] = makeClone ? layer.Clone() : layer;
            }

            layerManager.BoundingRectangle = Rectangle.Empty;

            return layerManager;
        }

        /// <summary>
        /// Reallocate with add size
        /// </summary>
        /// <returns></returns>
        public void Reallocate(uint insertAtLayerIndex, uint layerCount, bool initBlack = false)
        {
            var layers = Layers;
            Layers = new Layer[Count + layerCount];

            // Rearrange
            for (uint layerIndex = 0; layerIndex < insertAtLayerIndex; layerIndex++)
            {
                Layers[layerIndex] = layers[layerIndex];
            }

            // Rearrange
            for (uint layerIndex = insertAtLayerIndex; layerIndex < layers.Length; layerIndex++)
            {
                Layers[layerCount + layerIndex] = layers[layerIndex];
                Layers[layerCount + layerIndex].Index = layerCount + layerIndex;
            }

            // Allocate new layers
            if (initBlack)
            {
                Parallel.For(insertAtLayerIndex, insertAtLayerIndex + layerCount, layerIndex =>
                {
                    Layers[layerIndex] = new Layer((uint) layerIndex, EmguExtensions.InitMat(SlicerFile.Resolution), this);
                });
            }
            /*for (uint layerIndex = insertAtLayerIndex; layerIndex < insertAtLayerIndex + layerCount; layerIndex++)
            {
                Layers[layerIndex] = initBlack ? new Layer(layerIndex, EmguExtensions.InitMat(SlicerFile.Resolution), this) : null;
            }*/
        }

        public void ReallocateRange(uint startLayerIndex, uint endLayerIndex)
        {
            var layers = Layers;
            if ((int)(endLayerIndex - startLayerIndex) < 0) return;
            Layers = new Layer[1 + endLayerIndex - startLayerIndex];

            uint currentLayerIndex = 0;
            for (uint layerIndex = startLayerIndex; layerIndex <= endLayerIndex; layerIndex++)
            {
                Layers[currentLayerIndex++] = layers[layerIndex];
            }
            
            BoundingRectangle = Rectangle.Empty;
        }

        /// <summary>
        /// Reallocate at start
        /// </summary>
        /// <returns></returns>
        public void ReallocateStart(uint layerCount, bool initBlack = false) => Reallocate(0, layerCount, initBlack);

        /// <summary>
        /// Reallocate at end
        /// </summary>
        /// <returns></returns>
        public void ReallocateEnd(uint layerCount, bool initBlack = false) => Reallocate(Count, layerCount, initBlack);

        /// <summary>
        /// Clone this object
        /// </summary>
        /// <returns></returns>
        public LayerManager Clone()
        {
            LayerManager layerManager = new LayerManager(Count, SlicerFile);
            foreach (var layer in this)
            {
                layerManager[layer.Index] = layer.Clone();
            }
            layerManager.BoundingRectangle = BoundingRectangle;
            
            return layerManager;
        }

        public void Dispose() { }

        #endregion

        #region Formater

        public override string ToString()
        {
            return $"{nameof(BoundingRectangle)}: {BoundingRectangle}, {nameof(Count)}: {Count}, {nameof(IsModified)}: {IsModified}";
        }

        #endregion

        
    }
}
