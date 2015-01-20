using System;
using KSP;
using UnityEngine;
using KSPPluginFramework;

namespace TestFlightCore
{
    public class Settings : ConfigNodeStorage
    {
        public Settings(String FilePath) : base(FilePath) {
        }


        [Persistent] public bool debugLog = false;
        [Persistent] public double minTimeBetweenDataPoll = 0.5;
        [Persistent] public double minTimeBetweenFailurePoll = 60;
        [Persistent] public bool processAllVessels = false;
        [Persistent] public double flightDataMultiplier = 1.0;
        [Persistent] public double flightDataEngineerMultiplier = 1.0;
        [Persistent] public double globalReliabilityModifier = 1.0;
        [Persistent] public double masterStatusUpdateFrequency = 0.25;
        [Persistent] public bool displaySettingsWindow = false;
        [Persistent] public int settingsPage = 0;
        [Persistent] public bool enableHUD = false;
        [Persistent] public bool showFailedPartsOnlyInMSD = false;
        [Persistent] public bool showFlightDataInMSD = true;
        [Persistent] public bool showMTBFStringInMSD = true;
        [Persistent] public bool showFailureRateInMSD = false;
        [Persistent] public bool showStatusTextInMSD = true;
        [Persistent] public bool shortenPartNameInMSD = false;
        [Persistent] public bool mainWindowLocked = true;
        [Persistent] public bool editorWindowLocked = true;
        [Persistent] public int currentMSDSize = 1;
        [Persistent] public bool flightHUDEnabled = false;
        [Persistent] public bool editorShowGraph = false;
        [Persistent] public bool editorShowOnDemand = true;

        // Unity/KSP can't store some more complex data types so we provide classes to convert
        [Persistent] public PersistentRect mainWindowPositionStored = new PersistentRect();
        public Rect mainWindowPosition = new Rect(0,0,0,0);
        [Persistent] public PersistentVector2 currentMSDScrollPositionStored = new PersistentVector2();
        public Vector2 currentMSDScrollPosition = new Vector2(0,0);
        [Persistent] public PersistentVector2 currentEditorScrollPositionStored = new PersistentVector2();
        public Vector2 currentEditorScrollPosition = new Vector2(0,0);
        [Persistent] public PersistentRect flightHUDPositionStored = new PersistentRect();
        public Rect flightHUDPosition = new Rect(100,50,0,0);
        [Persistent] public PersistentRect editorWindowPositionStored = new PersistentRect();
        public Rect editorWindowPosition = new Rect(250,100,0,0);

        public override void OnDecodeFromConfigNode()
        {
            mainWindowPosition = mainWindowPositionStored.ToRect();
            editorWindowPosition = editorWindowPositionStored.ToRect();
            flightHUDPosition = flightHUDPositionStored.ToRect();
            currentMSDScrollPosition = currentMSDScrollPositionStored.ToVector2();
            currentEditorScrollPosition = currentEditorScrollPositionStored.ToVector2();
        }

        public override void OnEncodeToConfigNode()
        {
            mainWindowPositionStored = mainWindowPositionStored.FromRect(mainWindowPosition);
            editorWindowPositionStored = editorWindowPositionStored.FromRect(editorWindowPosition);
            flightHUDPositionStored = flightHUDPositionStored.FromRect(flightHUDPosition);
            currentMSDScrollPositionStored = currentMSDScrollPositionStored.FromVector2(currentMSDScrollPosition);
            currentEditorScrollPositionStored = currentEditorScrollPositionStored.FromVector2(currentMSDScrollPosition);
        }

    }


    public class PersistentVector2 : ConfigNodeStorage
    {
        [Persistent] public float x;
        [Persistent] public float y;

        public Vector2 ToVector2() 
        {
            return new Vector2(x, y);
        }

        public PersistentVector2 FromVector2(Vector2 vectorToStore)
        {
            this.x = vectorToStore.x;
            this.y = vectorToStore.y;
            return this;
        }
    }

    public class PersistentRect : ConfigNodeStorage
    {
        [Persistent] public float x;
        [Persistent] public float y;
        [Persistent] public float width;
        [Persistent] public float height;

        public Rect ToRect()
        { 
            return new Rect(x, y, width, height); 
        }
        public PersistentRect FromRect(Rect rectToStore)
        {
            this.x = rectToStore.x;
            this.y = rectToStore.y;
            this.width = rectToStore.width;
            this.height = rectToStore.height;
            return this;
        }
    }
}

