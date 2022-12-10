using System;
using System.Collections.Generic;

using Microsoft.Kinect;
using Microsoft.Kinect.VisualGestureBuilder;
using VL.Lib.Collections;

    /// <summary>
    /// Gesture Detector class which listens for VisualGestureBuilderFrame events from the service
    /// and updates the associated GestureResultView object with the latest results for the gesture
    /// </summary>

namespace VL.Devices.Kinect2
{
   

    public class GestureDetector : IDisposable
    {
         /// <summary> Path to the gesture database that was trained with VGB </summary>
        private readonly string gestureDatabase = @"lib\GestureDatabase\SwipeGestures.gbd"; 

        /// <summary> Name of the discrete gesture in the database that we want to track </summary>
        private readonly string SwipeLeft = "Swipe_Left";
        //private readonly string SwipeRight = "Swipe_Right";

        /// <summary> Gesture frame source which should be tied to a body tracking ID </summary>
        private VisualGestureBuilderFrameSource vgbFrameSource = null;

        /// <summary> Gesture frame reader which will handle gesture events coming from the sensor </summary>
        private VisualGestureBuilderFrameReader vgbFrameReader = null;

         /// <summary>
        /// Initializes a new instance of the GestureDetector class along with the gesture frame source and reader
        /// </summary>

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor kinectSensor = null;
        //private GestureResultView kinectSensor = null;
        private GestureResultView gestureResultView = null; 

        public GestureDetector(KinectSensor kinectSensor, GestureResultView gestureResultView)
        {

            this.gestureResultView = gestureResultView;


            // create the vgb source. The associated body tracking ID will be set when a valid body frame arrives from the sensor.
            this.vgbFrameSource = new VisualGestureBuilderFrameSource(kinectSensor, 0);
            this.vgbFrameSource.TrackingIdLost += this.Source_TrackingIdLost;

            // open the reader for the vgb frames
            this.vgbFrameReader = this.vgbFrameSource.OpenReader();
            if (this.vgbFrameReader != null)
            {
                this.vgbFrameReader.IsPaused = true;
                this.vgbFrameReader.FrameArrived += this.Reader_GestureFrameArrived;
            }

            // load the gesture from the gesture database
            using (VisualGestureBuilderDatabase database = new VisualGestureBuilderDatabase(this.gestureDatabase))
            {
                // we could load all available gestures in the database with a call to vgbFrameSource.AddGestures(database.AvailableGestures), 
                // but for this program, we only want to track one discrete gesture from the database, so we'll load it by name
                //foreach (Gesture gesture in database.AvailableGestures)
                //{
                //    if (gesture.Name.Equals(this.seatedGestureName))
                //    {
                //        this.vgbFrameSource.AddGesture(gesture);
                //    }
                //}

                this.vgbFrameSource.AddGestures(database.AvailableGestures);
            }
        }

        /// <summary> Gets the GestureResultView object which stores the detector results for display in the UI </summary>
        public GestureResultView GestureResult { get; private set; }

        /// <summary>
        /// Gets or sets the body tracking ID associated with the current detector
        /// The tracking ID can change whenever a body comes in/out of scope
        /// </summary>
        public ulong TrackingId
        {
            get
            {
                return this.vgbFrameSource.TrackingId;
            }

            set
            {
                if (this.vgbFrameSource.TrackingId != value)
                {
                    this.vgbFrameSource.TrackingId = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not the detector is currently paused
        /// If the body tracking ID associated with the detector is not valid, then the detector should be paused
        /// </summary>
        public bool IsPaused
        {
            get
            {
                return this.vgbFrameReader.IsPaused;
            }

            set
            {
                if (this.vgbFrameReader.IsPaused != value)
                {
                    this.vgbFrameReader.IsPaused = value;
                }
            }
        }

        public bool GestureRecognized
        {
            get
            {
                return this.GestureRecognized;
            }

            set
            {
                if (this.GestureRecognized != value)
                {
                    this.GestureRecognized = value;
                }
            }
        }

        /// <summary>
        /// Disposes all unmanaged resources for the class
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the VisualGestureBuilderFrameSource and VisualGestureBuilderFrameReader objects
        /// </summary>
        /// <param name="disposing">True if Dispose was called directly, false if the GC handles the disposing</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.vgbFrameReader != null)
                {
                    this.vgbFrameReader.FrameArrived -= this.Reader_GestureFrameArrived;
                    this.vgbFrameReader.Dispose();
                    this.vgbFrameReader = null;
                }

                if (this.vgbFrameSource != null)
                {
                    this.vgbFrameSource.TrackingIdLost -= this.Source_TrackingIdLost;
                    this.vgbFrameSource.Dispose();
                    this.vgbFrameSource = null;
                }
            }
        }

        /// <summary>
        /// Handles gesture detection results arriving from the sensor for the associated body tracking Id
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_GestureFrameArrived(object sender, VisualGestureBuilderFrameArrivedEventArgs e)
        {
            VisualGestureBuilderFrameReference frameReference = e.FrameReference;
            using (VisualGestureBuilderFrame frame = frameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    // get the discrete gesture results which arrived with the latest frame
                    IReadOnlyDictionary<Gesture, DiscreteGestureResult> discreteResults = frame.DiscreteGestureResults;
                    
                    if (discreteResults != null)
                    {
                         // we only have one gesture in this source object, but you can get multiple gestures
                        foreach (Gesture gesture in this.vgbFrameSource.Gestures)
                        {
                            //if (gesture.Name.Equals(this.seatedGestureName) && gesture.GestureType == GestureType.Discrete)
                            //{
                            //    DiscreteGestureResult result = null;
                            //    discreteResults.TryGetValue(gesture, out result);

                            //    if (result != null)
                            //    {
                            //        // update the GestureResultView object with new gesture result values
                            //        this.GestureResultView.UpdateGestureResult(true, result.Detected, result.Confidence);
                            //    }
                            //}

                            if (gesture.Name.Equals(this.SwipeLeft) && gesture.GestureType == GestureType.Discrete)
                            {
                                DiscreteGestureResult result = null;
                                discreteResults.TryGetValue(gesture, out result);

                                if (result != null)
                                {
                                    ///////////////////////////////////////////////
                                    //need to receieve values here !!!
                                    //////////////////////////////////////////////////
                                    // update the GestureResultView object with new gesture result values
                                    this.gestureResultView.UpdateGestureResult(true, result.Detected, result.Confidence);

                                    // update the GestureResultView object with new gesture result values
                                    //this.GestureResultView.UpdateGestureResult(true, result.Detected, result.Confidence);
                                }
                            }
                        }

                    }
                }
            }
        }

        /// <summary>
        /// Handles the TrackingIdLost event for the VisualGestureBuilderSource object
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Source_TrackingIdLost(object sender, TrackingIdLostEventArgs e)
        {
            // update the GestureResultView object to show the 'Not Tracked' image in the UI
            //this.GestureResultView.UpdateGestureResult(false, false, 0.0f);
        }

        /// <summary>
        /// Stores discrete gesture results for the GestureDetector.
        /// Properties are stored/updated for display in the UI.
        /// </summary>
        public sealed class GestureResultView
        {
            /// <summary> The body index (0-5) associated with the current gesture detector </summary>
            private int bodyIndex = 0;

            /// <summary> Current confidence value reported by the discrete gesture </summary>
            private float confidence = 0.0f;

            /// <summary> True, if the discrete gesture is currently being detected </summary>
            private bool detected = false;

            /// <summary> True, if the body is currently being tracked </summary>
            private bool isTracked = false;

            /// <summary>
            /// Initializes a new instance of the GestureResultView class and sets initial property values
            /// </summary>
            /// <param name="bodyIndex">Body Index associated with the current gesture detector</param>
            /// <param name="isTracked">True, if the body is currently tracked</param>
            /// <param name="detected">True, if the gesture is currently detected for the associated body</param>
            /// <param name="confidence">Confidence value for detection of the 'Seated' gesture</param>
            public GestureResultView(int bodyIndex, bool isTracked, bool detected, float confidence)
            {
                this.BodyIndex = bodyIndex;
                this.IsTracked = isTracked;
                this.Detected = detected;
                this.Confidence = confidence;
            }

            /// <summary> 
            /// Gets the body index associated with the current gesture detector result 
            /// </summary>
            public int BodyIndex
            {
                get
                {
                    return this.bodyIndex;
                }

                private set
                {
                    if (this.bodyIndex != value)
                    {
                        this.bodyIndex = value;

                    }
                }
            }

            /// <summary> 
            /// Gets a value indicating whether or not the body associated with the gesture detector is currently being tracked 
            /// </summary>
            public bool IsTracked
            {
                get
                {
                    return this.isTracked;
                }

                private set
                {
                    if (this.IsTracked != value)
                    {
                        this.isTracked = value;

                    }
                }
            }

            /// <summary> 
            /// Gets a value indicating whether or not the discrete gesture has been detected
            /// </summary>
            public bool Detected
            {
                get
                {
                    return this.detected;
                }

                private set
                {
                    if (this.detected != value)
                    {
                        this.detected = value;

                    }
                }
            }
            /// <summary> 
            /// Gets a float value which indicates the detector's confidence that the gesture is occurring for the associated body 
            /// </summary>
            public float Confidence
            {
                get
                {
                    return this.confidence;
                }

                private set
                {
                    if (this.confidence != value)
                    {
                        this.confidence = value;

                    }
                }
            }

            /// <summary>
            /// Updates the values associated with the discrete gesture detection result
            /// </summary>
            /// <param name="isBodyTrackingIdValid">True, if the body associated with the GestureResultView object is still being tracked</param>
            /// <param name="isGestureDetected">True, if the discrete gesture is currently detected for the associated body</param>
            /// <param name="detectionConfidence">Confidence value for detection of the discrete gesture</param>
            /// 

            public void UpdateGestureResult(bool isBodyTrackingIdValid, bool isGestureDetected, float detectionConfidence)
            {

                this.IsTracked = isBodyTrackingIdValid;
                this.Confidence = 0.0f;

                if (!this.IsTracked)
                {
                    this.Detected = false;
                }
                else
                {
                    this.Detected = isGestureDetected;

                    if (this.Detected)
                    {
                        this.Confidence = detectionConfidence;
                    }
                    else
                    {

                    }
                }


            }


        }

    }
}