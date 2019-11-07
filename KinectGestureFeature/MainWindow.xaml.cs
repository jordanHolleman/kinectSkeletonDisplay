﻿/*Mostly stolen from SKeletonBasics-WPF, Microsoft
 * 
 * */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
using Microsoft.Kinect.Toolkit;
using Microsoft.Kinect.Toolkit.Controls;
using Microsoft.Kinect.Toolkit.Fusion;
using Microsoft.Kinect.Toolkit.Interaction;
using System.IO;





namespace KinectGestureFeature
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //mostly stolen from SkeletonBasics-WPF, Microsoft
        //width & height of output display
        private const float RenderWidth = 640.0f;
        private const float RenderHeight = 480.0f;
        //bodyCenter thiccness
        private const double bodyCenterThickness = 40;
        //brushes! used for drawing body points
        private readonly Brush centerPointBrush = Brushes.Blue;
        private readonly Brush trackedPointBrush = Brushes.Green;
        private readonly Brush inferredPointBrush = Brushes.Red;
        //no bone tracking yet, save that for later
        //activate sensor
        private KinectSensor sensor;
        //drawing group for skeleton rendering
        private DrawingGroup drawingGroup;
        //drwawing image to be displayed
        private DrawingImage imageSource;
        private const double JointThickness = 10;

        private HandPointer activeHandPointer;

   
        public MainWindow()
        {
            InitializeComponent();
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            
            this.drawingGroup = new DrawingGroup();
            this.imageSource = new DrawingImage(this.drawingGroup);
            Image.Source = this.imageSource;
            //Image.SourceProperty = this.imageSource;

            //selecgting first connected sensor
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.sensor = potentialSensor;
                    break;
                }
            }

            if (this.sensor != null)
            {
                //turns on skelly stream to recieve skelly frames
                this.sensor.SkeletonStream.Enable();
                //add an event handler to be called whwnever there is new color frame data
                this.sensor.SkeletonFrameReady += this.SenesorSkeletonFrameReady;
                //start sensor
                try
                {
                    this.sensor.Start();
                }
                catch (IOException)
                {
                    //System.Diagnostics.Debug.WriteLine("Sensor's fucked, try again");
                    this.sensor = null;
                }

                if (null == this.sensor)
                {
                    System.Diagnostics.Debug.WriteLine("Sensor's fucked, try again");
                }

            }

        }

        //Event handler for kinect sensor's SkeletonFrameReady 
        private void SenesorSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            Skeleton[] skeletons = new Skeleton[0];
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);
                }
            }
            using (DrawingContext dc = this.drawingGroup.Open())
            {
                //draw background to set render size
                dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, RenderWidth, RenderHeight));
                if (skeletons.Length != 0)
                {
                    foreach (Skeleton skel in skeletons)
                    {
                        //render clipped edges, not implemented yet
                        //RenderClippedEdges(skel, dc);
                        if (skel.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            this.drawJoints(skel, dc);
                        }
                        else if (skel.TrackingState == SkeletonTrackingState.PositionOnly)
                        {
                            dc.DrawEllipse(this.centerPointBrush, null, this.SkeletonPointToScreen(skel.Position), bodyCenterThickness, bodyCenterThickness);
                        } 

                    }
                }
                //prevent drawing outside of rect area
                this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, RenderWidth, RenderHeight));
            }
        }

        private Point SkeletonPointToScreen(SkeletonPoint position)
        {
            //convert point to depth space
            DepthImagePoint depthPoint = this.sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(position, DepthImageFormat.Resolution640x480Fps30);
            return new Point(depthPoint.X, depthPoint.Y);
        }

        /**<summary>
         * draws joints
         * </summary>
         * <param name="skel">skellipoints to be drawn</param>
         * <param name="dc">drawing context to draw to</param>
         * */
        private void drawJoints(Skeleton skel, DrawingContext dc)
        {
            //render joints, bones TBA
            foreach (Joint joint in skel.Joints)
            {
                Brush drawBrush = null;
                if (joint.TrackingState==JointTrackingState.Tracked)
                {
                    drawBrush = this.trackedPointBrush;
                }
                else if (joint.TrackingState == JointTrackingState.Inferred)
                {
                    drawBrush = this.inferredPointBrush;
                }
                if (drawBrush != null)
                {
                    dc.DrawEllipse(drawBrush, null, this.SkeletonPointToScreen(joint.Position), JointThickness, JointThickness);
                }
            }
            //writing hands' x, y, and z values 
            Joint rightHand = skel.Joints[JointType.WristRight];
            rightHandXPosTextBox.Text = rightHand.Position.X.ToString();
            rightHandYPosYTextBox.Text = rightHand.Position.Y.ToString();
            rightHandZPosYTextBox.Text = rightHand.Position.Y.ToString();
            Joint leftHand = skel.Joints[JointType.WristLeft];
            leftHandXPosTextBox.Text = leftHand.Position.X.ToString();
            leftHandYPosTextBox.Text = leftHand.Position.Y.ToString();
            leftHandZPosTextBox.Text = leftHand.Position.Y.ToString();
            //tracking active user hands 
            //maybe the only original code written in here
            //just checks if the hand's x coord is higher than center hip's x coord
            Joint hipCenter = skel.Joints[JointType.HipCenter];
            
            if (rightHand.TrackingState == JointTrackingState.Tracked)
            {
                if (rightHand.Position.X > hipCenter.Position.X)
                {
                    rightHandActiveTextBox.Text = "yes";
                }
                else
                {
                    rightHandActiveTextBox.Text = "no";
                }

                if (skel.Joints[JointType.WristLeft].Position.X > skel.Joints[JointType.HipCenter].Position.X)
                {
                    leftHandActiveTextBox.Text = "yes";
                }
                else
                {
                    leftHandActiveTextBox.Text = "no";
                }
            }

            //This should track a cursor to the active hand, needs testing
            //selecting hand closest to sensor. Not sure if this is in the right place
            //source: https://github.com/Vangos/kinect-controls
            var activeHand = rightHand.Position.Z <= leftHand.Position.Z ? rightHand : leftHand;
            //get the hand's position relatively to the color image 
            var position = sensor.CoordinateMapper.MapSkeletonPointToColorPoint(activeHand.Position, ColorImageFormat.RgbResolution640x480Fps30);
            //var position with depth
            
            //flip the cursor to match the active hand and update its posiition
            cursor.Flip(activeHand);
            //cursor.Update(position);
            //depthImagePoint Update
            cursor.Update(position);

            /**
             * TODO something like
             * if (activeHandPointer.getPosition is within button1)
             *      if (activeHandPointer.isPressed or isGrippedInteraction)
             *              press button
             * not sure how this would look yet
             * */
             

        }
        
        

        
        
        
        
        
        
        
        







    }
}
