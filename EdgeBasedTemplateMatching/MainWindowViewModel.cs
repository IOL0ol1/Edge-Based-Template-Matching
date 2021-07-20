using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

using Microsoft.Win32;

using OpenCvSharp;

using Prism.Commands;
using Prism.Mvvm;

namespace EdgeBasedTemplateMatching
{
    public class MainWindowViewModel : BindableBase
    {
        public MainWindowViewModel()
        {
            LoadCommand = new DelegateCommand<string>(LoadExecute);
            TrainCommand = new DelegateCommand(TrainTemplate);
            SearchCommand = new DelegateCommand(MatchSearch);
        }

        public ICommand LoadCommand { get; private set; }
        public ICommand TrainCommand { get; private set; }
        public ICommand SearchCommand { get; private set; }

        private Mat template;

        /// <summary>
        /// Image used to create template
        /// </summary>
        public Mat Template
        {
            get { return template; }
            set
            {
                template = value;
                RaisePropertyChanged();
            }
        }

        private Mat destination;

        /// <summary>
        /// Image to be processed
        /// </summary>
        public Mat Destination
        {
            get { return destination; }
            set
            {
                destination = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Load template and destination image
        /// </summary>
        /// <param name="i"></param>
        private void LoadExecute(string i)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.InitialDirectory = Path.Combine( Path.GetDirectoryName(GetType().Assembly.Location),"Images");
            dialog.Filter = "JPG|*.jpg|PNG|*.png|BMP|*.bmp|All|*.*";
            if (dialog.ShowDialog().Value == true)
            {
                if (dialog.CheckFileExists)
                {
                    string file = dialog.FileName;
                    if (i.ToLower().Contains("t"))
                    {
                        Template?.Dispose();
                        Template = new Mat(file);
                    }
                    else
                    {
                        Destination?.Dispose();
                        Destination = new Mat(file);
                    }
                }
            }
        }

        public TrainParame TrainParame { get; } = new TrainParame();

        public SearchParame SearchParame { get; } = new SearchParame();


        /// <summary>
        /// Result data
        /// </summary>
        private List<PointInfo> results = new List<PointInfo>();



        /// <summary>
        /// Create a template
        /// </summary>
        private void TrainTemplate()
        {
            try
            {
                results.Clear();
                using (Mat src = new Mat())
                using (Mat output = new Mat())
                using (Mat gx = new Mat())
                using (Mat gy = new Mat())
                using (Mat magnitude = new Mat())
                using (Mat direction = new Mat())
                {
                    /// convert to gray image
                    Cv2.CvtColor(template, src, ColorConversionCodes.RGB2GRAY);

                    /// using the canny algorithm to get edges
                    Cv2.Canny(src, output, TrainParame.Threshold1, TrainParame.Threshold2, TrainParame.ApertureSize, TrainParame.L2gradient);
                    Cv2.FindContours(output, out var contours, out var hierarchy, TrainParame.Mode, TrainParame.Method);


                    /// use the sobel filter on the template image which returns the gradients in the X (Gx) and Y (Gy) direction.
                    Cv2.Sobel(src, gx, MatType.CV_64F, 1, 0, 3);
                    Cv2.Sobel(src, gy, MatType.CV_64F, 0, 1, 3);

                    /// compute the magnitude and direction(radians)
                    Cv2.CartToPolar(gx, gy, magnitude, direction);

                    /// save edge info
                    var sum = new Point2d(0, 0);
                    for (int i = 0, m = contours.Length; i < m; i++)
                    {
                        for (int j = 0, n = contours[i].Length; j < n; j++)
                        {
                            var cur = contours[i][j];
                            var fdx = gx.At<double>(cur.Y, cur.X, 0); // dx
                            var fdy = gy.At<double>(cur.Y, cur.X, 0); // dy
                            var der = new Point2d(fdx, fdy); // (dx,dy)
                            var mag = magnitude.At<double>(cur.Y, cur.X, 0); // √(dx²+dy²)
                            var dir = direction.At<double>(cur.Y, cur.X, 0); // atan2(dy,dx)
                            results.Add(new PointInfo
                            {
                                Point = cur,
                                Derivative = der,
                                Direction = dir,
                                Magnitude = mag == 0 ? 0 : 1 / mag,
                            });
                            sum += cur;
                        }
                    }

                    /// update Center and Offset in PointInfo
                    var center = new Point2d(sum.X / results.Count, sum.Y / results.Count);
                    foreach (var item in results)
                    {
                        item.Update(center);
                    }

                    /// overlay display origin image, edge(green) and center point(red)
                    Cv2.DrawContours(template, new[] { results.Select(_ => _.Point) }, -1, Scalar.LightGreen, 2);
                    //Cv2.DrawContours(template, contours, -1, Scalar.LightGreen, 2);
                    Cv2.Circle(template, center.ToPoint(), 2, Scalar.Red, -1);
                }

                /// update UI
                RaisePropertyChanged(nameof(Template));
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message);
                Trace.TraceError(ex.StackTrace);
            }
        }


        /// <summary>
        /// NCC to find template
        /// </summary>
        private void MatchSearch()
        {
            Stopwatch stopwatch = new Stopwatch();
            try
            {
                Trace.TraceInformation("NCC matching start");
                stopwatch.Start();

                /// convert to gray image
                using (Mat src = new Mat())
                using (Mat gx = new Mat())
                using (Mat gy = new Mat())
                using (Mat direction = new Mat())
                using (Mat magnitude = new Mat())
                {

                    Cv2.CvtColor(destination, src, ColorConversionCodes.RGB2GRAY);

                    /// use the sobel filter on the source image which returns the gradients in the X (Gx) and Y (Gy) direction.
                    Cv2.Sobel(src, gx, MatType.CV_64F, 1, 0, 3);
                    Cv2.Sobel(src, gy, MatType.CV_64F, 0, 1, 3);

                    /// compute the magnitude and direction
                    Cv2.CartToPolar(gx, gy, magnitude, direction);

                    var minScore = SearchParame.MinScore;
                    var greediness = SearchParame.Greediness;

                    /// ncc match search
                    long noOfCordinates = results.Count;
                    double normMinScore = minScore / noOfCordinates; // normalized min score
                    double normGreediness = (1 - greediness * minScore) / (1 - greediness) / noOfCordinates;
                    double partialScore = 0;
                    double resultScore = 0;
                    Point center = new Point();

                    for (int i = 0, h = src.Height; i < h; i++)
                    {
                        for (int j = 0, w = src.Width; j < w; j++)
                        {
                            double partialSum = 0;
                            for (var m = 0; m < noOfCordinates; m++)
                            {
                                var item = results[m];
                                var curX = (int)(j + item.Offset.X);
                                var curY = (int)(i + item.Offset.Y);
                                var iTx = item.Derivative.X;
                                var iTy = item.Derivative.Y;
                                if (curX < 0 || curY < 0 || curY > src.Height - 1 || curX > src.Width - 1)
                                    continue;

                                var iSx = gx.At<double>(curY, curX, 0);
                                var iSy = gy.At<double>(curY, curX, 0);

                                if ((iSx != 0 || iSy != 0) && (iTx != 0 || iTy != 0))
                                {
                                    var mag = magnitude.At<double>(curY, curX, 0);
                                    var matGradMag = mag == 0 ? 0 : 1 / mag; // 1/√(dx²+dy²)
                                    partialSum += ((iSx * iTx) + (iSy * iTy)) * (item.Magnitude * matGradMag);
                                }

                                var sumOfCoords = m + 1;
                                partialScore = partialSum / sumOfCoords;
                                /// check termination criteria
                                /// if partial score score is less than the score than needed to make the required score at that position
                                /// break serching at that coordinate.
                                if (partialScore < Math.Min((minScore - 1) + normGreediness * sumOfCoords, normMinScore * sumOfCoords))
                                    break;
                            }
                            if (partialScore > resultScore)
                            {
                                resultScore = partialScore;
                                center.X = j;
                                center.Y = i;
                            }
                        }
                    }

                    /// overlay display origin image, edge(green) and center point(red)
                    Cv2.DrawContours(destination, new[] { results.Select(_ => _.Offset.ToPoint()) }, -1, Scalar.LightGreen, 2, offset: center);
                    Cv2.Circle(destination, center, 5, Scalar.Red, -1);
                    Trace.TraceInformation($"NCC matching score {resultScore}. time: {stopwatch.Elapsed.TotalMilliseconds} ms");
                }

                RaisePropertyChanged(nameof(Destination));
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message);
                Trace.TraceError(ex.StackTrace);
            }
            finally
            {
                stopwatch.Stop();
            }
        }

    }

    /// <summary>
    /// Train parame
    /// </summary>
    public class TrainParame : BindableBase
    {

        private double threshold1 = 10;
        /// <summary>
        /// Canny threshold1
        /// </summary>
        public double Threshold1
        {
            get { return threshold1; }
            set { SetProperty(ref threshold1, value); }
        }

        public double threshold2 = 100;
        /// <summary>
        /// Canny threshold2
        /// </summary>
        public double Threshold2
        {
            get { return threshold2; }
            set { SetProperty(ref threshold2, value); }
        }

        private int apertureSize = 3;
        /// <summary>
        /// Canny apertureSize
        /// </summary>
        public int ApertureSize
        {
            get { return apertureSize; }
            set { SetProperty(ref apertureSize, value); }
        }

        /// <summary>
        /// Canny L2gradient
        /// </summary>
        private bool l2gradient = false;
        public bool L2gradient
        {
            get { return l2gradient; }
            set { SetProperty(ref l2gradient, value); }
        }

        /// <summary>
        /// FindContours mode
        /// </summary>
        private RetrievalModes mode = RetrievalModes.External;
        public RetrievalModes Mode
        {
            get { return mode; }
            set { SetProperty(ref mode, value); }
        }

        /// <summary>
        /// FindContours method
        /// </summary>
        private ContourApproximationModes method = ContourApproximationModes.ApproxNone;
        public ContourApproximationModes Method
        {
            get { return method; }
            set { SetProperty(ref method, value); }
        }

    }

    /// <summary>
    /// Search parame
    /// </summary>
    public class SearchParame : BindableBase
    {
        private double minScore = 0.9;
        /// <summary>
        /// min score to skip search
        /// </summary>
        public double MinScore
        {
            get { return minScore; }
            set { SetProperty(ref minScore, value); }
        }

        private double greediness = 0.9;
        /// <summary>
        /// greediness for search
        /// </summary>
        public double Greediness
        {
            get { return greediness; }
            set { SetProperty(ref greediness, value); }
        }

    }

    /// <summary>
    /// Point information.
    /// </summary>
    /// <remarks>
    /// ----→  dx
    /// |\ ）m
    /// | \
    /// |  \
    /// ↓   \
    /// dy    c
    ///
    /// Derivative = (dx,dy)
    /// Magnitude = 1/c = 1/√(dx²+dy²)
    /// Direction = atan2(dy,dx) (not currently in use)
    /// </remarks>
    public class PointInfo
    {
        /// <summary>
        /// Point of edge 
        /// </summary>
        /// <remarks>
        /// (x,y)
        /// </remarks>
        public Point Point;

        /// <summary>
        /// Center of edge 
        /// </summary>
        /// <remarks>
        /// (x0,y0)
        /// </remarks>
        public Point2d Center { get; private set; }

        /// <summary>
        /// Point-Center 
        /// </summary>
        /// <remarks>
        /// (x-x0,y-y0)
        /// </remarks>
        public Point2d Offset { get; private set; }

        /// <summary>
        /// Derivative at Point
        /// </summary>
        /// <remarks>
        /// (dx,dy)
        /// </remarks>
        public Point2d Derivative;

        /// <summary>
        /// Magnitude at Point
        /// </summary>
        /// <remarks>
        /// 1/√(dx²+dy²)
        /// </remarks>
        public double Magnitude;

        /// <summary>
        /// Direction at Point
        /// </summary>
        /// <remarks>
        /// atan2(dy,dx) (not currently in use)
        /// </remarks>
        public double Direction;

        /// <summary>
        /// Calc Offset with Point by center
        /// </summary>
        /// <param name="center"></param>
        public void Update(Point2d center)
        {
            Center = center;
            Offset = Point - center;
        }
    }


}
