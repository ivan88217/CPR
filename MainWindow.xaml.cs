
namespace Microsoft.Samples.Kinect.BodyBasics
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Windows;  
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect;
    using System.Timers;
    using System.Runtime.InteropServices;
    using System.Windows.Media;
    using System.Windows.Input;
    using System.Windows.Threading;
    /// <summary> 
    /// Interaction logic for MainWindow
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    { 
        private const int MapDepthToByte = 8000 / 256;

        private DepthFrameReader depthFrameReader = null;

        private FrameDescription depthFrameDescription = null;

        private WriteableBitmap depthBitmap = null;



        private byte[] depthPixels = null;

        private enum KeyEventFlag
        {
            KEYEVENTF_KEYDOWN = 0x0000,
            KEYEVENTF_EXTENDEDKEY = 0x0001,
            KEYEVENTF_KEYUP = 0x0002,
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern void keybd_event(
            byte bVk, byte bScan, KeyEventFlag dwFlags,
            IntPtr dwExtraInfo);
        private Timer startTimer = new Timer(3500);

        public delegate void Timerdelegate();

        private const double HandSize = 30;

        private const double JointThickness = 3;

        private const double ClipBoundsThickness = 5;

        private const float InferredZPositionClamp = 0.1f;

        private readonly Brush handClosedBrush = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));

        private readonly Brush handOpenBrush = new SolidColorBrush(Color.FromArgb(128, 0, 255, 0));
        
        private readonly Brush handLassoBrush = new SolidColorBrush(Color.FromArgb(128, 0, 0, 255));

        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));
 
        private readonly Brush inferredJointBrush = Brushes.Yellow;
        
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);
                
        private DrawingGroup drawingGroup;

        private DrawingImage bodyimageSource;

        private KinectSensor kinectSensor = null;

        private CoordinateMapper coordinateMapper = null;

        private BodyFrameReader bodyFrameReader = null;

        private Body[] bodies = null;

        private List<Tuple<JointType, JointType>> bones;

        private int displayWidth;
     
        private int displayHeight;
      
        private List<Pen> bodyColors;

        private string statusText = null; // Initializes a new instance of the MainWindow class.

        private ColorFrameReader colorFrameReader = null;
        private WriteableBitmap colorBitmap = null;


        public MainWindow()
        {          
            startTimer.Elapsed += new System.Timers.ElapsedEventHandler(timeout);
            // one sensor is currently supported
            this.kinectSensor = KinectSensor.GetDefault();

            this.depthFrameReader = this.kinectSensor.DepthFrameSource.OpenReader();

            this.depthFrameReader.FrameArrived += this.Reader_FrameArrived2;

            // get FrameDescription from DepthFrameSource
            this.depthFrameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;

            // allocate space to put the pixels being received and converted
            this.depthPixels = new byte[this.depthFrameDescription.Width * this.depthFrameDescription.Height];

            // create the bitmap to display
            this.depthBitmap = new WriteableBitmap(this.depthFrameDescription.Width, this.depthFrameDescription.Height, 96.0, 96.0, PixelFormats.Gray8, null);




            // get the coordinate mapper
            this.coordinateMapper = this.kinectSensor.CoordinateMapper;

            this.colorFrameReader = this.kinectSensor.ColorFrameSource.OpenReader();
             this.colorFrameReader.FrameArrived += this.Reader_ColorFrameArrived;
            // get the depth (display) extents
             FrameDescription frameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;
            FrameDescription colorFrameDescription = this.kinectSensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);
            this.colorBitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);
            // get size of joint space
            this.displayWidth = frameDescription.Width;
            this.displayHeight = frameDescription.Height;
           
            // open the reader for the body frames
            this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();

            // a bone defined as a line between two joints
            this.bones = new List<Tuple<JointType, JointType>>();

            // Torso
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Head, JointType.Neck));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Neck, JointType.SpineShoulder));
           // this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.SpineMid));
            //this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineMid, JointType.SpineBase));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderLeft));
            //this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipRight));
            //this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipLeft));

            // Right Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderRight, JointType.ElbowRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowRight, JointType.WristRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.HandRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandRight, JointType.HandTipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.ThumbRight));

            // Left Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderLeft, JointType.ElbowLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowLeft, JointType.WristLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.HandLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandLeft, JointType.HandTipLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.ThumbLeft));

            // Right Leg
            //this.bones.Add(new Tuple<JointType, JointType>(JointType.HipRight, JointType.KneeRight));
            //this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeRight, JointType.AnkleRight));
            //this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleRight, JointType.FootRight));

            // Left Leg
            //this.bones.Add(new Tuple<JointType, JointType>(JointType.HipLeft, JointType.KneeLeft));
            //this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeLeft, JointType.AnkleLeft));
            //this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleLeft, JointType.FootLeft));

            // populate body colors, one for each BodyIndex
            this.bodyColors = new List<Pen>();

            this.bodyColors.Add(new Pen(Brushes.Red, 6));
            this.bodyColors.Add(new Pen(Brushes.Orange, 6));
            this.bodyColors.Add(new Pen(Brushes.Green, 6));
            this.bodyColors.Add(new Pen(Brushes.Blue, 6));
            this.bodyColors.Add(new Pen(Brushes.Indigo, 6));
            this.bodyColors.Add(new Pen(Brushes.Violet, 6));

            // set IsAvailableChanged event notifier
            this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

            // open the sensor
            this.kinectSensor.Open();

            // set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.NoSensorStatusText;

            // Create the drawing group we'll use for drawing
            this.drawingGroup = new DrawingGroup();

            // Create an image source that we can use in our image control
            this.bodyimageSource = new DrawingImage(this.drawingGroup);

            // use the window object as the view model in this simple example
            this.DataContext = this;

            // initialize the components (controls) of the window
            this.InitializeComponent();
        }
    private void SendKeyDown(Key key)
        {
            keybd_event(
                (byte)KeyInterop.VirtualKeyFromKey(key), 0,
                KeyEventFlag.KEYEVENTF_KEYDOWN, IntPtr.Zero);
        }

        /// <summary>
        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;


        public ImageSource bodylmage
        {
            get
            {
                return this.bodyimageSource;               
            }
        }
        public ImageSource ImageSource
        {
            get
            {               
                return this.colorBitmap;
            }
        }
      
        public string StatusText
        {
            get
            {
                return this.statusText;
            }

            set
            {
                if (this.statusText != value)
                {
                    this.statusText = value;

                    // notify any bound elements that the text has changed
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
                    }
                }
            }
        }

     
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            
            if (this.bodyFrameReader != null)
            {
                this.bodyFrameReader.FrameArrived+= this.Reader_FrameArrived;           
            }
            if (this.depthFrameReader != null)
            {
                this.depthFrameReader.FrameArrived += this.Reader_FrameArrived2;
            }
        }

   
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (this.depthFrameReader != null)
            {
                // DepthFrameReader is IDisposable
                this.depthFrameReader.Dispose();
                this.depthFrameReader = null;
            }

            if (this.bodyFrameReader != null)
            {
                // BodyFrameReader is IDisposable
                this.bodyFrameReader.Dispose();
                this.bodyFrameReader = null;
            }

            if (this.kinectSensor != null)
            {
                
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
        }
        bool Headplay = true;
        private void Reader_FrameArrived2(object sender, DepthFrameArrivedEventArgs e)
        {
            if (depthPixels[(depthFrameDescription.Height / 2) * depthFrameDescription.Width + (depthFrameDescription.Width / 2)] > 30
                   && depthPixels[(depthFrameDescription.Height / 2) * depthFrameDescription.Width + (depthFrameDescription.Width / 2)] < 70)
            {
                Headplay = true;
            }
            else Headplay = false;

            bool depthFrameProcessed = false;

            using (DepthFrame depthFrame = e.FrameReference.AcquireFrame())
            {
                if (depthFrame != null)
                {
                    // the fastest way to process the body index data is to directly access 
                    // the underlying buffer
                    using (Microsoft.Kinect.KinectBuffer depthBuffer = depthFrame.LockImageBuffer())
                    {
                        // verify data and write the color data to the display bitmap
                        if (((this.depthFrameDescription.Width * this.depthFrameDescription.Height) == (depthBuffer.Size / this.depthFrameDescription.BytesPerPixel)) &&
                            (this.depthFrameDescription.Width == this.depthBitmap.PixelWidth) && (this.depthFrameDescription.Height == this.depthBitmap.PixelHeight))
                        {
                            // Note: In order to see the full range of depth (including the less reliable far field depth)
                            // we are setting maxDepth to the extreme potential depth threshold
                            ushort maxDepth = ushort.MaxValue;

                            // If you wish to filter by reliable depth distance, uncomment the following line:
                            //// maxDepth = depthFrame.DepthMaxReliableDistance

                            this.ProcessDepthFrameData(depthBuffer.UnderlyingBuffer, depthBuffer.Size, depthFrame.DepthMinReliableDistance, maxDepth);
                            depthFrameProcessed = true;
                        }
                    }
                }
            }

            if (depthFrameProcessed)
            {
                this.RenderDepthPixels();
            }
        }
        private unsafe void ProcessDepthFrameData(IntPtr depthFrameData, uint depthFrameDataSize, ushort minDepth, ushort maxDepth)
        {
            // depth frame data is a 16 bit value
            ushort* frameData = (ushort*)depthFrameData;

            // convert depth to a visual representation
            for (int i = 0; i < (int)(depthFrameDataSize / this.depthFrameDescription.BytesPerPixel); ++i)
            {
                // Get the depth for this pixel
                ushort depth = frameData[i];

                // To convert to a byte, we're mapping the depth value to the byte range.
                // Values outside the reliable depth range are mapped to 0 (black).
                this.depthPixels[i] = (byte)(depth >= minDepth && depth <= maxDepth ? (depth / MapDepthToByte) : 0);
            }
        }

        /// <summary>
        /// Renders color pixels into the writeableBitmap.
        /// </summary>
        private void RenderDepthPixels()
        {
            this.depthBitmap.WritePixels(
                new Int32Rect(0, 0, this.depthBitmap.PixelWidth, this.depthBitmap.PixelHeight),
                this.depthPixels,
                this.depthBitmap.PixelWidth,
                0);
        }


        bool lefthand = false;
        bool righthand = false;
        
        private void Reader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            bool player = true;
            bool dataReceived = false;

            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null )
                {
                    if (this.bodies == null ) 
                    {
                        this.bodies = new Body[bodyFrame.BodyCount];
                    }
                   
                    // The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
                    // As long as those body objects are not disposed and not set to null in the array,
                    // those body objects will be re-used.
                    bodyFrame.GetAndRefreshBodyData(this.bodies);
                    dataReceived = true;
                }
            }
            if (dataReceived)
            {
                using (DrawingContext dc = this.drawingGroup.Open())
                {
                    // Draw a transparent background to set the render size
                    dc.DrawRectangle(Brushes.Transparent, null, new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));                  
                    Pen line = new Pen(Brushes.Red, 1);
                   // Pen line2 = new Pen(Brushes.Red, 4);
                    dc.DrawLine(line, new Point(0, 335), new Point(600, 335));
                   // dc.DrawLine(line2, new Point(ElbowRightX, ElbowRightY), new Point(ElbowRightX , ElbowRightY+2));
                   // dc.DrawEllipse(EllipseBrush, null, new Point(350, 232), 5, 5);
                    int penIndex = 0;
                        foreach (Body body in this.bodies)
                        {  
                            Pen drawPen = this.bodyColors[penIndex++];
                           /* if (!Headplay) { Pen drawPen = this.bodyColors[penIndex--]; }
                            else { Pen drawPen = this.bodyColors[penIndex++]; }*/
                            if (body.IsTracked && player && Headplay)
                            {
                                player = false;
                                //this.DrawClippedEdges(body, dc);

                                IReadOnlyDictionary<JointType, Joint> joints = body.Joints;

                                // convert the joint points to depth (display) space

                                Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>();

                                foreach (JointType jointType in joints.Keys)
                                {
                                    // sometimes the depth(Z) of an inferred joint may show as negative
                                    // clamp down to 0.1f to prevent coordinatemapper from returning (-Infinity, -Infinity)
                                    CameraSpacePoint position = joints[jointType].Position;
                                    if (position.Z < 0)
                                    {
                                        position.Z = InferredZPositionClamp;
                                    }
                                    DepthSpacePoint depthSpacePoint = this.coordinateMapper.MapCameraPointToDepthSpace(position);
                                    jointPoints[jointType] = new Point(depthSpacePoint.X, depthSpacePoint.Y);
                                }
                                ProcessGesture(jointPoints);
                                //depthGOGO(jointPoints);
                                this.DrawBody(joints, jointPoints, dc, drawPen);
                                this.DrawHand(body.HandLeftState, jointPoints[JointType.HandLeft], dc);
                                this.DrawHand(body.HandRightState, jointPoints[JointType.HandRight], dc);
                                if (jointPoints[JointType.HandLeft].X > (jointPoints[JointType.ShoulderRight].X) + 10 && righthand == false && start)
                                {
                                    righthand = true; 
                                    lefthand = false;
                                   SendKeyDown(Key.PageUp);
                                }
                                if (jointPoints[JointType.HandLeft].X < jointPoints[JointType.ShoulderRight].X && jointPoints[JointType.HandRight].X > jointPoints[JointType.ShoulderLeft].X && start)
                                {
                                    righthand = false;
                                    lefthand = false;
                                }
                                if (jointPoints[JointType.HandRight].X < (jointPoints[JointType.ShoulderLeft].X) - 10 && lefthand == false && start)
                                {
                                    lefthand = true;
                                    righthand = false;
                                    SendKeyDown(Key.PageDown);
                                }
                            }
                        }
                    
                    // prevent drawing outside of our render area
                    this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));
                }
            }

        }

      
        private void DrawBody(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, DrawingContext drawingContext, Pen drawingPen)
        {
            // Draw the bones
            foreach (var bone in this.bones)
            {
                this.DrawBone(joints, jointPoints, bone.Item1, bone.Item2, drawingContext, drawingPen);
            }

            // Draw the joints
            foreach (JointType jointType in joints.Keys)
            {
                Brush drawBrush = null;

                TrackingState trackingState = joints[jointType].TrackingState;
                
                if (trackingState == TrackingState.Tracked)
                {
                    drawBrush = this.trackedJointBrush;
                }
                else if (trackingState == TrackingState.Inferred)
                {
                    drawBrush = this.inferredJointBrush;
                }

                /*if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, jointPoints[jointType], JointThickness, JointThickness);
                }*/
            }
        }

       
        private void DrawBone(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, JointType jointType0, JointType jointType1, DrawingContext drawingContext, Pen drawingPen)
        {
            Joint joint0 = joints[jointType0];
            Joint joint1 = joints[jointType1];
            
            // If we can't find either of these joints, exit
            if (joint0.TrackingState == TrackingState.NotTracked ||
                joint1.TrackingState == TrackingState.NotTracked)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.inferredBonePen;
            if ((joint0.TrackingState == TrackingState.Tracked) && (joint1.TrackingState == TrackingState.Tracked))
            {
                drawPen = drawingPen;
            }

            drawingContext.DrawLine(drawPen, jointPoints[jointType0], jointPoints[jointType1]);
        }
        private void DrawHand(HandState handState, Point handPosition, DrawingContext drawingContext)
        {
            switch (handState)
            {
                case HandState.Closed:
                    start = false;
                    statusReset();
                    drawingContext.DrawEllipse(this.handClosedBrush, null, handPosition, HandSize, HandSize);
                    break;

                case HandState.Open:
                    start = true;                     
                    drawingContext.DrawEllipse(this.handOpenBrush, null, handPosition, HandSize, HandSize);
                    break;

                 case HandState.Lasso:
                    start2 = true;
                    drawingContext.DrawEllipse(this.handLassoBrush, null, handPosition, HandSize, HandSize);
                     break;
            }
        }
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            // on failure, set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.SensorNotAvailableStatusText;
        }
        private void Reader_ColorFrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            // ColorFrame is IDisposable
            using (ColorFrame colorFrame = e.FrameReference.AcquireFrame())
            {
                if (colorFrame != null)
                {
                    FrameDescription colorFrameDescription = colorFrame.FrameDescription;

                    using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
                    {
                        this.colorBitmap.Lock();                       
                        // verify data and write the new color frame data to the display bitmap
                        if ((colorFrameDescription.Width == this.colorBitmap.PixelWidth) && (colorFrameDescription.Height == this.colorBitmap.PixelHeight))
                        {
                            colorFrame.CopyConvertedFrameDataToIntPtr(
                                this.colorBitmap.BackBuffer,
                                (uint)(colorFrameDescription.Width * colorFrameDescription.Height * 4),
                                ColorImageFormat.Bgra);

                            this.colorBitmap.AddDirtyRect(new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight));
                        }

                        this.colorBitmap.Unlock();
                    }
                }
            }
        }
        bool start = false;
        bool start2 = false;
        bool isRightDown = false;
        bool isLeftDown = false;
        bool isCPRStart = false;
        bool isCPRStartHandPositionLock = false;
        int count = 0;
        bool error = false;
        double LeftShoulderUpPosition;
        double RightShoulderUpPosition;
        double LeftShoulderUpPositionX;
        double RightShoulderUpPositionX;  
        bool isCount = false;
        System.Media.SoundPlayer sp = new System.Media.SoundPlayer(); 
        private void ProcessGesture(Dictionary<JointType, Point> jointPoints)
        {
          //  PositionError(jointPoints);
            if(!isCPRStartHandPositionLock)
            {                    
                LeftShoulderUpPosition = jointPoints[JointType.ShoulderLeft].Y;
                RightShoulderUpPosition = jointPoints[JointType.ShoulderRight].Y;
                LeftShoulderUpPositionX = jointPoints[JointType.ShoulderLeft].X;
                RightShoulderUpPositionX = jointPoints[JointType.ShoulderRight].X;
            }
            if (!isCPRStart )
            {
               // PositionError(jointPoints);
                if (start && start2)
                {
                    if (start && jointPoints[JointType.HandTipRight].X - jointPoints[JointType.HandTipLeft].X < 60 && jointPoints[JointType.ShoulderLeft].Y + jointPoints[JointType.ShoulderRight].Y >250)
                              {                               
                                  Status.Foreground = new SolidColorBrush(Colors.Green);
                                Status.Content = "開始";
                                _2green.Visibility = Visibility.Visible;
                                _3green.Visibility = Visibility.Visible;
                                startTimer.Start();                              
                              }  else
                             {
                                 _1green.Visibility = Visibility.Visible;
                                  Status.Foreground = new SolidColorBrush(Colors.Green);
                                 Status.Content = "開始雙手合併，並檢查身體位置";
                              }                                            
                }
                else 
                {          
                     statusReset();                 
                }
            }        
            else
            {
               
                Status.Content = PositionError(jointPoints);             
                if ( LeftShoulderUpPosition > jointPoints[JointType.ShoulderLeft].Y)//現在座標較上限小(往下)
                {
                    double positionTemp1 = LeftShoulderUpPosition - jointPoints[JointType.ShoulderLeft].Y;

                    if (positionTemp1 > 3) //處理抖動
                        {

                            LDownPositionLabel.Content = "左手  " + Math.Round(positionTemp1,2);
                            if (isLeftDown == false)
                            {
                                isLeftDown = true;
                            }                           
                        }


                    if (LeftShoulderUpPosition - 5.5 < jointPoints[JointType.ShoulderLeft].Y)
                    {
                        LDownPositionLabel.Content = "左手向上  ";                        
                        if (isLeftDown == true)
                        {                           
                            isLeftDown = false;                            
                        }                       
                    }                 
                }
                if ( RightShoulderUpPosition > jointPoints[JointType.ShoulderRight].Y)//現在座標較上限小(往下)
                {
                    double positionTemp2 = RightShoulderUpPosition - jointPoints[JointType.ShoulderRight].Y;


                    if (positionTemp2 > 3) //處理抖動
                    {
                           
                         RDownPositionLabel.Content = "右手  " + Math.Round(positionTemp2,2);
                         if (isRightDown == false)
                         {
                                isRightDown = true;
                         }
                    }

                    if (RightShoulderUpPosition - 5.5 < jointPoints[JointType.ShoulderRight].Y)
                    {
                        RDownPositionLabel.Content = "右手向上  ";
                        if (isRightDown == true)
                        {                           
                             //count = count + 1;
                             //CPRCount.Content = count;                            
                            //RightHandUpPosition = jointPoints[JointType.HandRight].Y;
                            isRightDown = false;
                        }
                    }          
                }               
                if(isRightDown == true && isLeftDown == true && !error)
                {
                    if (isCount == false )
                    {
                        count = count + 1;
                        CPRCount.Content = count + "次";
                        isCount = true;
                    }
                }
                if (isRightDown == false && isLeftDown == false)
                {
                    isCount = false;
                }
             }
        }
        public void timeout(object source, System.Timers.ElapsedEventArgs e)
        {
           
            sp.SoundLocation = @"C:\Users\偉勝\Desktop\CPR-Beta-1.0\CPR-Beta-1.0\cpr開始(鯰魚)剪接.wav";
            sp.Play();
            startTimer.Stop();
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Timerdelegate(CPRStartFun));
        }
        private void CPRStartFun()
        {
            _4green.Visibility = Visibility.Visible;
                isCPRStart = true;
                isCPRStartHandPositionLock = true;                      
        }
        bool sperror=true;
        private string PositionError(Dictionary<JointType, Point> jointPoints)
        {
            Status.Foreground = new SolidColorBrush(Colors.Red);
            if (jointPoints[JointType.ShoulderLeft].Y + jointPoints[JointType.ShoulderRight].Y < LeftShoulderUpPosition + RightShoulderUpPosition-80
                  || jointPoints[JointType.ShoulderLeft].Y + jointPoints[JointType.ShoulderRight].Y > LeftShoulderUpPosition + RightShoulderUpPosition+80
                  || jointPoints[JointType.ShoulderLeft].X < LeftShoulderUpPositionX - 60 || jointPoints[JointType.ShoulderRight].X > RightShoulderUpPositionX + 60
                || jointPoints[JointType.ShoulderLeft].Y - jointPoints[JointType.ShoulderRight].Y > LeftShoulderUpPosition - RightShoulderUpPosition + 20
                || jointPoints[JointType.ShoulderLeft].Y - jointPoints[JointType.ShoulderRight].Y < LeftShoulderUpPosition - RightShoulderUpPosition - 20
               || jointPoints[JointType.ShoulderLeft].X - jointPoints[JointType.ElbowLeft].X > 12 || jointPoints[JointType.ShoulderRight].X - jointPoints[JointType.ElbowRight].X < -12)
            {          
               if(sperror) 
               {
                sp.Stop();
                sp.SoundLocation = @"C:\Users\偉勝\Desktop\CPR-Beta-1.0\CPR-Beta-1.0\姿勢不良(鯰魚).wav";
                sp.Play();
                sperror = false;
               }
                  if (jointPoints[JointType.ShoulderLeft].X - jointPoints[JointType.ElbowLeft].X > 14)
                {
                    return "姿勢不良:左手未打直";
                }
                else if (jointPoints[JointType.ShoulderRight].X - jointPoints[JointType.ElbowRight].X < -14)
                {
                    return "姿勢不良:右手未打直";
                }
                if (jointPoints[JointType.ShoulderLeft].X < LeftShoulderUpPositionX -60 || jointPoints[JointType.ShoulderRight].X > RightShoulderUpPositionX+60
                    || jointPoints[JointType.ShoulderLeft].Y + jointPoints[JointType.ShoulderRight].Y < LeftShoulderUpPosition + RightShoulderUpPosition - 80
                  || jointPoints[JointType.ShoulderLeft].Y + jointPoints[JointType.ShoulderRight].Y > LeftShoulderUpPosition + RightShoulderUpPosition + 80)
                {
                   return "姿勢不良:身體位置不正確";
                }
                else if (jointPoints[JointType.ShoulderLeft].Y - jointPoints[JointType.ShoulderRight].Y > LeftShoulderUpPosition - RightShoulderUpPosition+20)
                {
                    return "姿勢不良:肩膀姿勢不端正,右肩太高";
                }

                else 
                {
                    return "姿勢不良:肩膀姿勢不端正,左肩太高";
                }

            }
            else
            {
                /*if (jointPoints[JointType.ShoulderLeft].X - jointPoints[JointType.ElbowLeft].X > 12 )
                {
                    return "姿勢不良:左手未打直";
                }
                else if (jointPoints[JointType.ShoulderRight].X - jointPoints[JointType.ElbowRight].X < -12 )
                {
                    return "姿勢不良:右手未打直";
                }
                else
                {*/
                sperror = true;
                Status.Foreground = new SolidColorBrush(Colors.Green);
                error = false;
               return  "開始";
                }
            }
                  

   
        private void statusReset()
        {
            Status.Foreground = new SolidColorBrush(Colors.Red);
                isRightDown = false;
                isLeftDown = false;
                isCPRStart = false;
                isCPRStartHandPositionLock = false;
                count = 0;           
                startTimer.Stop();
                CPRCount.Content = "CPR";
                LDownPositionLabel.Content = "重置";
                RDownPositionLabel.Content = "重置";
                Status.Content = "重置";
                start = false;
                start2 = false;
                sp.Stop();
                _1green.Visibility = Visibility.Hidden;
                _2green.Visibility = Visibility.Hidden;
                _3green.Visibility = Visibility.Hidden;
                _4green.Visibility = Visibility.Hidden;
                _5green.Visibility = Visibility.Hidden;

                }

        private void Explanation_Click(object sender, RoutedEventArgs e)
        {
            CPRExplanation frm = new CPRExplanation();
            frm.Show();
        }
   /*     int RightShoulderY;
        int RightShoulderX;
        int ElbowRightY;
        int ElbowRightX;
        int HeadY;
        int HeadX;
        private void depthGOGO(Dictionary<JointType, Point> jointPoints)
        {

           /* try
            {
                HeadX=Convert.ToInt32(jointPoints[JointType.Head].X);
                HeadY=Convert.ToInt32(jointPoints[JointType.Head].Y);
                RightShoulderY = Convert.ToInt32(jointPoints[JointType.ShoulderRight].Y-1);
                RightShoulderX = Convert.ToInt32(jointPoints[JointType.ShoulderRight].X);
                ElbowRightY = Convert.ToInt32(jointPoints[JointType.ElbowRight].Y+20);
                ElbowRightX = Convert.ToInt32(jointPoints[JointType.ElbowRight].X-5);
                short RightShoulderdepth = depthPixels[RightShoulderY * depthFrameDescription.Width + RightShoulderX];
                short ElbowRightdepth = depthPixels[ElbowRightY * depthFrameDescription.Width + ElbowRightX];
                Label2.Content = ElbowRightdepth;
                if (ElbowRightdepth - RightShoulderdepth > 2 || RightShoulderdepth-ElbowRightdepth > 2)
                {
                    Label.Content = RightShoulderdepth + "姿勢不良";
                }
                else
                {
                    Label.Content = RightShoulderdepth + "    ";
                }
                if (depthPixels[HeadY * depthFrameDescription.Width + HeadX]>70)
                {
                    this.kinectSensor.Close();
                    this.kinectSensor.Open();
                }
                }
            catch
            {

            }*/
        }      
}
    
