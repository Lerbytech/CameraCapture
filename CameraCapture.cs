//----------------------------------------------------------------------------
//  Copyright (C) 2004-2015 by EMGU Corporation. All rights reserved.       
//----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.Util;
using Emgu.CV.Util;

namespace CameraCapture
{
   public partial class CameraCapture : Form
   {
      private Capture _capture = null;
      private bool _captureInProgress;

      public CameraCapture()
      {
         InitializeComponent();
         CvInvoke.UseOpenCL = false;
         try
         {
            _capture = new Capture();
            _capture.ImageGrabbed += ProcessFrame;
         }
         catch (NullReferenceException excpt)
         {
            MessageBox.Show(excpt.Message);
         }
      }

      private void ProcessFrame(object sender, EventArgs arg)
      {
         Mat frame = new Mat();
         //_capture.Retrieve(frame, 0);
         frame = new Mat("C:\\Emgu\\Dump\\ea6b5b28a66c.jpg", LoadImageType.Unchanged);
         Mat grayFrame = new Mat();
         CvInvoke.CvtColor(frame, grayFrame, ColorConversion.Bgr2Gray);
         Mat smallGrayFrame = new Mat();
         CvInvoke.PyrDown(grayFrame, smallGrayFrame);
         Mat smoothedGrayFrame = new Mat();
         CvInvoke.PyrUp(smallGrayFrame, smoothedGrayFrame);

         CvInvoke.Threshold(smoothedGrayFrame, smoothedGrayFrame, 100, 255, ThresholdType.Binary);
         //Image<Gray, Byte> smallGrayFrame = grayFrame.PyrDown();
         //Image<Gray, Byte> smoothedGrayFrame = smallGrayFrame.PyrUp();
         Mat cannyFrame = new Mat();
         CvInvoke.Canny(smoothedGrayFrame, cannyFrame, 100, 60);

         //Image<Gray, Byte> cannyFrame = smoothedGrayFrame.Canny(100, 60);

         VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
         CvInvoke.FindContours(cannyFrame, contours, null, RetrType.List, ChainApproxMethod.ChainApproxSimple);
         CvInvoke.DrawContours(frame, contours, 2, new Bgr(Color.Blue).MCvScalar);
         List<RotatedRect> BL = new List<RotatedRect>();
         List<VectorOfPoint> CL = new List<VectorOfPoint>();
         for (int i = 0; i < contours.Size; i++)
         {
           using (VectorOfPoint contour = contours[i])
           using (VectorOfPoint approxContour = new VectorOfPoint())
           {
             CvInvoke.ApproxPolyDP(contour, approxContour, CvInvoke.ArcLength(contour, true) * 0.05, true);
             BL.Add(CvInvoke.MinAreaRect(approxContour));
             CL.Add(contour);
           }
         }

         VectorOfPoint maxContour = CL[0];
        double maxContourArea = CvInvoke.ContourArea(CL[0], false);
         for (int i = 0; i < CL.Count; i++)
         {
           if (CvInvoke.ContourArea(CL[i], false) > maxContourArea)
           {
             maxContourArea = CvInvoke.ContourArea(CL[i], false);
             maxContour = CL[i];
           }
         }

         RotatedRect TMP = new RotatedRect();
         TMP = CvInvoke.MinAreaRect(maxContour);
         CvInvoke.Polylines(frame, Array.ConvertAll(TMP.GetVertices(), Point.Round), true, new Bgr(Color.Pink).MCvScalar, 2);

         
         Image<Bgr, Byte> srcImg = frame.ToImage<Bgr, Byte>();
         srcImg.ROI = new Rectangle((int)(TMP.Center.X - 0.5 * TMP.Size.Width), (int)(TMP.Center.Y - 0.5 * TMP.Size.Height), (int)TMP.Size.Width, (int)TMP.Size.Height);
         Image<Bgr, Byte> croppedImg = srcImg.Copy();
         cannyImageBox.Image = croppedImg;
         float[,] tmp = {
                            {0, frame.Height}, //down
                            {0, 0},//left
                            {frame.Width, 0}, // up
                            {frame.Width, frame.Height} //right
                       };
         Matrix<float> sourceMat = new Matrix<float>(tmp);
         float[,] target = {
                            {0, (float)0.85 * frame.Height},
                            {0, 0},
                            {(float)0.85*frame.Width, 0},
                            {(float)0.55*frame.Width, (float)0.55*frame.Height}
                       };
         PointF[] tmpPF = new PointF[4];
         PointF[] targetPF = new PointF[4];

         for (int i = 0; i < 4; i++)
         {
           tmpPF[i].X = tmp[i, 0]; tmpPF[i].Y = tmp[i, 1];
           targetPF[i].X = target[i, 0]; targetPF[i].Y = target[i, 1];
         }

         Matrix<float> targetMat = new Matrix<float>(target);
         Mat TTT = CvInvoke.GetPerspectiveTransform(tmpPF, targetPF);
         Mat newcroppimg = new Mat();
         CvInvoke.WarpPerspective(croppedImg, newcroppimg, TTT, new System.Drawing.Size(241, 240));

        
        
        //CvInvoke.DrawContours(frame, TMP, 2, new Bgr(Color.Red).MCvScalar);
         
         /*
        foreach (RotatedRect box in BL)
         {
           CvInvoke.Polylines(frame, Array.ConvertAll(box.GetVertices(), Point.Round), true, new Bgr(Color.DarkOrange).MCvScalar, 2);
         }*/

         captureImageBox.Image = frame;
         grayscaleImageBox.Image = newcroppimg;
         smoothedGrayscaleImageBox.Image = smoothedGrayFrame;
         //cannyImageBox.Image = cannyFrame;
      }

      private void captureButtonClick(object sender, EventArgs e)
      {
         if (_capture != null)
         {
            if (_captureInProgress)
            {  //stop the capture
               captureButton.Text = "Start Capture";
               _capture.Pause();
            }
            else
            {
               //start the capture
               captureButton.Text = "Stop";
               _capture.Start();
            }

            _captureInProgress = !_captureInProgress;
         }
      }

      private void ReleaseData()
      {
         if (_capture != null)
            _capture.Dispose();
      }
   
      private void FlipHorizontalButtonClick(object sender, EventArgs e)
      {
          if (_capture != null) _capture.FlipHorizontal = !_capture.FlipHorizontal;
      }

      private void FlipVerticalButtonClick(object sender, EventArgs e)
      {
         if (_capture != null) _capture.FlipVertical = !_capture.FlipVertical;
      }
   }
}
































/*
 * //----------------------------------------------------------------------------
//  Copyright (C) 2004-2015 by EMGU Corporation. All rights reserved.       
//----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.Util;

namespace CameraCapture
{
   public partial class CameraCapture : Form
   {
      private Capture _capture = null;
      private bool _captureInProgress;

      public CameraCapture()
      {
         InitializeComponent();
         CvInvoke.UseOpenCL = false;
         try
         {
            _capture = new Capture();
            _capture.ImageGrabbed += ProcessFrame;
         }
         catch (NullReferenceException excpt)
         {
            MessageBox.Show(excpt.Message);
         }
      }

      private void ProcessFrame(object sender, EventArgs arg)
      {
         Mat frame = new Mat();
         _capture.Retrieve(frame, 0);
         //frame = new Mat("C:\\Emgu\\Dump\\TestWarpImage.png", LoadImageType.Unchanged);
         Mat grayFrame = new Mat();
         CvInvoke.CvtColor(frame, grayFrame, ColorConversion.Bgr2Gray);
         Mat smallGrayFrame = new Mat();
         CvInvoke.PyrDown(grayFrame, smallGrayFrame);
         Mat smoothedGrayFrame = new Mat();
         CvInvoke.PyrUp(smallGrayFrame, smoothedGrayFrame);
         
         //Image<Gray, Byte> smallGrayFrame = grayFrame.PyrDown();
         //Image<Gray, Byte> smoothedGrayFrame = smallGrayFrame.PyrUp();
         Mat cannyFrame = new Mat();
         CvInvoke.Canny(smoothedGrayFrame, cannyFrame, 100, 60);

         //Image<Gray, Byte> cannyFrame = smoothedGrayFrame.Canny(100, 60);

         captureImageBox.Image = frame;
         grayscaleImageBox.Image = grayFrame;
         smoothedGrayscaleImageBox.Image = smoothedGrayFrame;
         cannyImageBox.Image = cannyFrame;
      }

      private void captureButtonClick(object sender, EventArgs e)
      {
         if (_capture != null)
         {
            if (_captureInProgress)
            {  //stop the capture
               captureButton.Text = "Start Capture";
               _capture.Pause();
            }
            else
            {
               //start the capture
               captureButton.Text = "Stop";
               _capture.Start();
            }

            _captureInProgress = !_captureInProgress;
         }
      }

      private void ReleaseData()
      {
         if (_capture != null)
            _capture.Dispose();
      }

      private void FlipHorizontalButtonClick(object sender, EventArgs e)
      {
         if (_capture != null) _capture.FlipHorizontal = !_capture.FlipHorizontal;
      }

      private void FlipVerticalButtonClick(object sender, EventArgs e)
      {
         if (_capture != null) _capture.FlipVertical = !_capture.FlipVertical;
      }
   }
}

*/