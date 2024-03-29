﻿/* Part of KSPPluginFramework
Version 1.2

Forum Thread:http://forum.kerbalspaceprogram.com/threads/66503-KSP-Plugin-Framework
Author: TriggerAu, 2014
License: The MIT License (MIT)
*/

/*
 * Equivelant of MonobehaviourExtended for PartModule
 * Original code provided by TriggerAu in KSPPluginFramework
 * PartModuleExtended and PartModuleWindow written by Agathorn
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using KSP;
using UnityEngine;

namespace TestFlightCore.KSPPluginFramework
{
    /// <summary>
    /// An Extended version of the UnityEngine.PartModule Class
    /// Has some added functions to simplify repeated use and some defined overridable functions for common functions
    /// </summary>
    public abstract class PartModuleExtended : PartModule
    {
        #region RepeatingFunction Code
        private Boolean _RepeatRunning = false;
        /// <summary>
        /// Returns whether the RepeatingWorkerFunction is Running
        /// </summary>
        internal Boolean RepeatingWorkerRunning { get { return _RepeatRunning; } }

        //Storage Variables
        private Single _RepeatInitialWait;
        private Single _RepeatSecs;

        /// <summary>
        /// Get/Set the period in seconds that the repeatingfunction is triggered at
        /// Note: When setting this value if the repeating function is already running it restarts it to set the new period
        /// </summary>
        internal Single RepeatingWorkerRate
        {
            get { return _RepeatSecs; }
            private set
            {
                LogFormatted_DebugOnly("Setting RepeatSecs to {0}", value);
                _RepeatSecs = value;
                //If its running then restart it
                if (RepeatingWorkerRunning)
                {
                    StopRepeatingWorker();
                    StartRepeatingWorker();
                }
            }
        }

        /// <summary>
        /// Set the repeating period by how many times a second it should repeat
        ///    eg. if you set this to 4 then it will repeat every 0.25 secs
        /// </summary>
        /// <param name="NewTimesPerSecond">Number of times per second to repeat</param>
        /// <returns>The new RepeatSecs value (eg 0.25 from the example)</returns>
        internal Single SetRepeatTimesPerSecond(Int32 NewTimesPerSecond)
        {
            RepeatingWorkerRate = (Single)(1 / (Single)NewTimesPerSecond);
            return RepeatingWorkerRate;
        }
        /// <summary>
        /// Set the repeating period by how many times a second it should repeat
        ///    eg. if you set this to 4 then it will repeat every 0.25 secs
        /// </summary>
        /// <param name="NewTimesPerSecond">Number of times per second to repeat</param>
        /// <returns>The new RepeatSecs value (eg 0.25 from the example)</returns>
        internal Single SetRepeatTimesPerSecond(Single NewTimesPerSecond)
        {
            RepeatingWorkerRate = (Single)(1 / NewTimesPerSecond);
            return RepeatingWorkerRate;
        }
        /// <summary>
        /// Set the repeating rate in seconds for the repeating function
        ///    eg. if you set this to 0.1 then it will repeat 10 times every second
        /// </summary>
        /// <param name="NewSeconds">Number of times per second to repeat</param>
        /// <returns>The new RepeatSecs value</returns>
        internal Single SetRepeatRate(Single NewSeconds)
        {
            RepeatingWorkerRate = NewSeconds;
            return RepeatingWorkerRate;
        }

        /// <summary>
        /// Get/Set the value of the period that should be waited before the repeatingfunction begins
        /// eg. If you set this to 1 and then start the repeating function then the first time it fires will be in 1 second and then every RepeatSecs after that
        /// </summary>
        internal Single RepeatingWorkerInitialWait
        {
            get { return _RepeatInitialWait; }
            set { _RepeatInitialWait = value; }
        }

        #region Start/Stop Functions
        /// <summary>
        /// Starts the RepeatingWorker Function and sets the TimesPerSec variable
        /// </summary>
        /// <param name="TimesPerSec">How many times a second should the RepeatingWorker Function be run</param>
        /// <returns>The RunningState of the RepeatinWorker Function</returns>
        internal Boolean StartRepeatingWorker(Int32 TimesPerSec)
        {
            LogFormatted_DebugOnly("Starting the repeating function");
            //Stop it if its running
            StopRepeatingWorker();
            //Set the new value
            SetRepeatTimesPerSecond(TimesPerSec);
            //Start it and return the result
            return StartRepeatingWorker();
        }

        /// <summary>
        /// Starts the Repeating worker
        /// </summary>
        /// <returns>The RunningState of the RepeatinWorker Function</returns>
        internal Boolean StartRepeatingWorker()
        {
            try
            {
                LogFormatted_DebugOnly("Invoking the repeating function");
                this.InvokeRepeating("RepeatingWorkerWrapper", _RepeatInitialWait, RepeatingWorkerRate);
                _RepeatRunning = true;
            }
            catch (Exception)
            {
                LogFormatted("Unable to invoke the repeating function");
                //throw;
            }
            return _RepeatRunning;
        }

        /// <summary>
        /// Stop the RepeatingWorkerFunction
        /// </summary>
        /// <returns>The RunningState of the RepeatinWorker Function</returns>
        internal Boolean StopRepeatingWorker()
        {
            try
            {
                LogFormatted_DebugOnly("Cancelling the repeating function");
                this.CancelInvoke("RepeatingWorkerWrapper");
                _RepeatRunning = false;
            }
            catch (Exception)
            {
                LogFormatted("Unable to cancel the repeating function");
                //throw;
            }
            return _RepeatRunning;
        }
        #endregion

        /// <summary>
        /// Function that is repeated.
        /// You can monitor the duration of the execution of your RepeatingWorker using RepeatingWorkerDuration 
        /// You can see the game time that passes between repeats via RepeatingWorkerUTPeriod
        /// 
        /// No Need to run the base RepeatingWorker
        /// </summary>
        internal virtual void RepeatingWorker()
        {
            //LogFormatted_DebugOnly("WorkerBase");

        }

        /// <summary>
        /// Time that the last iteration of RepeatingWorkerFunction ran for. Can use this value to see how much impact your code is having
        /// </summary>
        internal TimeSpan RepeatingWorkerDuration { get; private set; }


        /// <summary>
        /// The Game Time that the Repeating Worker function last started
        /// </summary>
        private Double RepeatingWorkerUTLastStart { get; set; }
        /// <summary>
        /// The Game Time that the Repeating Worker function started this time
        /// </summary>
        private Double RepeatingWorkerUTStart { get; set; }
        /// <summary>
        /// The amount of UT that passed between the last two runs of the Repeating Worker Function
        /// 
        /// NOTE: Inside the RepeatingWorker Function this will be the UT that has passed since the last run of the RepeatingWorker
        /// </summary>
        internal Double RepeatingWorkerUTPeriod { get; private set; }

        /// <summary>
        /// This is the wrapper function that calls all the repeating function goodness
        /// </summary>
        private void RepeatingWorkerWrapper()
        {
            //record the start date
            DateTime Duration = DateTime.Now;

            //Do the math to work out how much game time passed since last time
            RepeatingWorkerUTLastStart = RepeatingWorkerUTStart;
            RepeatingWorkerUTStart = Planetarium.GetUniversalTime();
            RepeatingWorkerUTPeriod = RepeatingWorkerUTStart - RepeatingWorkerUTLastStart;

            //Now call the users code function as they will have overridden this
            RepeatingWorker();

            //Now calc the duration
            RepeatingWorkerDuration = (DateTime.Now - Duration);
        }
        #endregion

        #region Standard Monobehaviour definitions-for overriding
        // PartModule has 6 standard overrides and can use any standard Unity MonoBehaviour EXCEPT Awake()
        // See this for info on order of execuction
        //  http://docs.unity3d.com/Documentation/Manual/ExecutionOrder.html

        /// <summary>
        /// Awake is not allowed for PartModule
        /// </summary>
        //internal virtual void Awake()
        //{
        //    LogFormatted_DebugOnly("New PMExtended Awakened");
        //}

        /// <summary>
        /// KSP:
        ///     Constructor style setup.
        ///     Called in the Part's Awake method. 
        ///     The model may not be built by this point.
        /// </summary>
        public override void OnAwake()
        {
            LogFormatted_DebugOnly("PMExtended OnAwake");
        }

        /// <summary>
        /// Unity: Start is called on the frame when a script is enabled just before any of the Update methods is called the first time.
        ///
        /// Trigger: This is the last thing that happens before the scene starts doing stuff
        ///          See this for info on order of execuction: http://docs.unity3d.com/Documentation/Manual/ExecutionOrder.html
        /// </summary>
        public virtual void Start()
        {
            LogFormatted_DebugOnly("New MBExtended Started");
        }

        /// <summary>
        /// KSP:
        ///     Called during the Part startup.
        ///     StartState gives flag values of initial state
        /// </summary>
        /// <param name="state">Initial state</param>
        public override void OnStart(StartState state)
        {
            LogFormatted_DebugOnly("New PMExtended OnStart");
        }

        /// <summary>
        /// Unity: This function is called every fixed framerate frame, if the MonoBehaviour is enabled.
        ///
        /// Trigger: This Update is called at a fixed rate and usually where you do all your physics stuff for consistent results
        ///          See this for info on order of execuction: http://docs.unity3d.com/Documentation/Manual/ExecutionOrder.html
        ///          
        /// Agathorn:
        ///     For PartModule, FixedUpdate() is called every physics update even if the part is not active.  In most cases
        ///     you will want to use OnFixedUpdate() instead.
        /// </summary>
        public virtual void FixedUpdate()
        { }

        /// <summary>
        /// KSP:
        ///     Per-physx-frame update
        ///     Called ONLY when Part is ACTIVE!
        /// </summary>
        public override void OnFixedUpdate()
        { }

        /// <summary>
        /// Unity: LateUpdate is called every frame, if the MonoBehaviour is enabled.
        ///
        /// Trigger: This Update is called just before the rendering, and where you can adjust any graphical values/positions based on what has been updated in the physics, etc
        ///          See this for info on order of execuction: http://docs.unity3d.com/Documentation/Manual/ExecutionOrder.html
        /// </summary>
        public virtual void LateUpdate()
        { }

        /// <summary>
        /// Unity: Update is called every frame, if the MonoBehaviour is enabled.
        ///
        /// Trigger: This is usually where you stick all your control inputs, keyboard handling, etc
        ///          See this for info on order of execuction: http://docs.unity3d.com/Documentation/Manual/ExecutionOrder.html
        ///          
        /// Agathorn: Note that for PartModule, Update() will be called every frame regardless of scene or state.  In otherwords
        ///         This will be called for example in the VAB/SPH scenes when a part is added to a vessel.  It will also be called
        ///         in the Flight scene even if the part is not yet active.  In *most* cases you should be using OnUpdate() instead.
        /// </summary>
        public virtual void Update()
        { }

        /// <summary>
        /// KSP:
        ///     Per-frame update
        ///     Called ONLY when Part is ACTIVE!
        ///     
        /// Agathorn: 
        ///     For PartModule OnUpdate() is similiar to Update() in that it is called every frame, however OnUpdate() is only
        ///     called when the part is *active*.
        /// </summary>
        public override void OnUpdate()
        { }

        /// <summary>
        /// KSP:
        ///     Called when PartModule is asked to save its values.
        ///     Can save additional data here.
        /// </summary>
        /// <param name="node">The node to save in to</param>
        public override void OnSave(ConfigNode node)
        {
            LogFormatted_DebugOnly("PMExtended OnSave");
        }

        /// <summary>
        /// KSP:
        ///     Called when PartModule is asked to load its values.
        ///     Can load additional data here.
        /// </summary>
        /// <param name="node">The node to load from</param>
        public override void OnLoad(ConfigNode node)
        {
            LogFormatted_DebugOnly("PMExtended OnLoad");
        }

        /// <summary>
        /// Unity Help: This function is called when the MonoBehaviour will be destroyed..
        ///
        /// Trigger: Override this for destruction and cleanup code
        ///          See this for info on order of execuction: http://docs.unity3d.com/Documentation/Manual/ExecutionOrder.html
        /// </summary>
        public virtual void OnDestroy()
        {
            LogFormatted_DebugOnly("Destroying MBExtended");
        }

        #endregion

        #region Assembly/Class Information
        /// <summary>
        /// Name of the Assembly that is running this MonoBehaviour
        /// </summary>
        internal static String _AssemblyName
        { get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Name; } }

        /// <summary>
        /// Name of the Class - including Derivations
        /// </summary>
        internal String _ClassName
        { get { return this.GetType().Name; } }
        #endregion

        #region Logging
        /// <summary>
        /// Some Structured logging to the debug file - ONLY RUNS WHEN DLL COMPILED IN DEBUG MODE
        /// </summary>
        /// <param name="Message">Text to be printed - can be formatted as per String.format</param>
        /// <param name="strParams">Objects to feed into a String.format</param>
        [System.Diagnostics.Conditional("DEBUG")]
        internal static void LogFormatted_DebugOnly(String Message, params object[] strParams)
        {
            LogFormatted("DEBUG: " + Message, strParams);
        }

        /// <summary>
        /// Some Structured logging to the debug file
        /// </summary>
        /// <param name="Message">Text to be printed - can be formatted as per String.format</param>
        /// <param name="strParams">Objects to feed into a String.format</param>
        internal static void LogFormatted(String Message, params object[] strParams)
        {
            Message = String.Format(Message, strParams);                  // This fills the params into the message
            String strMessageLine = String.Format("{0},{2},{1}",
                DateTime.Now, Message,
                _AssemblyName);                                           // This adds our standardised wrapper to each line
            UnityEngine.Debug.Log(strMessageLine);                        // And this puts it in the log
        }

        #endregion
    }
}