using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;

using KSP;
using UnityEngine;
using KSPPluginFramework;

namespace TestFlight
{
    internal class Resources
    {
        //WHERE SHOULD THESE BE???
        internal static String PathApp = KSPUtil.ApplicationRootPath.Replace("\\", "/");
        internal static String PathTestFlight = string.Format("{0}GameData/TestFlight", PathApp);
        //internal static String PathPlugin = string.Format("{0}/{1}", PathTriggerTech, KSPAlternateResourcePanel._AssemblyName);
        internal static String PathPlugin = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        internal static String PathPluginResources = string.Format("{0}/Resources", PathTestFlight);
        internal static String PathPluginToolbarIcons = string.Format("{0}/Resources/ToolbarIcons", PathTestFlight);
        internal static String PathPluginTextures = string.Format("{0}/Resources/Textures", PathTestFlight);
        //internal static String PathPluginData = string.Format("{0}/Data", PathPlugin);
        internal static String PathPluginSounds = string.Format("{0}/Resources/Sounds", PathPlugin);

        internal static String DBPathTestFlight = string.Format("TestFlight");
        internal static String DBPathPlugin = string.Format("TestFlight/{0}", TestFlightCore.TestFlightWindow._AssemblyName);
        internal static String DBPathResources = string.Format("{0}/Resources", DBPathPlugin);
        internal static String DBPathToolbarIcons = string.Format("{0}/Resources/ToolbarIcons", DBPathPlugin);
        internal static String DBPathTextures = string.Format("{0}/Resources/Textures", DBPathPlugin);
        internal static String DBPathPluginSounds = string.Format("{0}/Resources/Sounds", DBPathPlugin);


        internal static Texture2D texPanel = new Texture2D(16, 16, TextureFormat.ARGB32, false);
        internal static Texture2D texBarBlue  = new Texture2D(13, 13, TextureFormat.ARGB32, false);
        internal static Texture2D texBarBlue_Back = new Texture2D(13, 13, TextureFormat.ARGB32, false);
        internal static Texture2D texBarGreen = new Texture2D(13, 13, TextureFormat.ARGB32, false);
        internal static Texture2D texBarGreen_Back = new Texture2D(13, 13, TextureFormat.ARGB32, false);

        internal static Texture2D texBarHighlight = new Texture2D(9, 9, TextureFormat.ARGB32, false);
        internal static Texture2D texBarHighlightGreen = new Texture2D(9, 9, TextureFormat.ARGB32, false);
        internal static Texture2D texBarHighlightRed = new Texture2D(9, 9, TextureFormat.ARGB32, false);

        internal static Texture2D btnChevronUp = new Texture2D(17, 16, TextureFormat.ARGB32, false);
        internal static Texture2D btnChevronDown = new Texture2D(17, 16, TextureFormat.ARGB32, false);

        internal static Texture2D btnChevronLeft = new Texture2D(38, 38, TextureFormat.ARGB32, false);
        internal static Texture2D btnChevronRight = new Texture2D(38, 38, TextureFormat.ARGB32, false);

        internal static Texture2D btnViewAll = new Texture2D(16, 16, TextureFormat.ARGB32, false);
        internal static Texture2D btnViewTimes = new Texture2D(16, 16, TextureFormat.ARGB32, false);

        internal static Texture2D btnSettingsAttention = new Texture2D(17, 16, TextureFormat.ARGB32, false);

        internal static Texture2D texPartWindowHead = new Texture2D(16, 16, TextureFormat.ARGB32, false);

        //internal static Texture2D texTooltipBackground; // = new Texture2D(9, 9);//, TextureFormat.ARGB32, false);

        internal static Texture2D texRateUp = new Texture2D(10, 10, TextureFormat.ARGB32, false);
        internal static Texture2D texRateDown = new Texture2D(10, 10, TextureFormat.ARGB32, false);

        internal static Texture2D btnAlarm = new Texture2D(16, 16, TextureFormat.ARGB32, false);
        internal static Texture2D btnAlarmEnabled = new Texture2D(16, 16, TextureFormat.ARGB32, false);
        internal static Texture2D btnAlarmWarn = new Texture2D(16, 16, TextureFormat.ARGB32, false);
        internal static Texture2D btnAlarmAlert = new Texture2D(16, 16, TextureFormat.ARGB32, false);

        //internal static Texture2D btnLock;
        //internal static Texture2D btnUnlock;

        internal static Texture2D btnDropDown = new Texture2D(10, 10, TextureFormat.ARGB32, false);
        internal static Texture2D btnPlay = new Texture2D(10, 10, TextureFormat.ARGB32, false);
        internal static Texture2D btnStop = new Texture2D(10, 10, TextureFormat.ARGB32, false);

        internal static Texture2D texResourceMove = new Texture2D(378, 9, TextureFormat.ARGB32, false);

        internal static Texture2D texBox = new Texture2D(9, 9, TextureFormat.ARGB32, false);
        internal static Texture2D texBoxUnity = new Texture2D(9, 9, TextureFormat.ARGB32, false);

        internal static Texture2D texSeparatorV = new Texture2D(6, 2, TextureFormat.ARGB32, false);
        internal static Texture2D texSeparatorH = new Texture2D(2, 20, TextureFormat.ARGB32, false);

        internal static Texture2D texAppLaunchIcon = new Texture2D(38, 38, TextureFormat.ARGB32, false);

        internal static void LoadTextures()
        {
            MonoBehaviourExtended.LogFormatted("Loading Textures");

            LoadImageFromFile(ref btnChevronLeft, "ChevronLeft", PathPluginResources);
            LoadImageFromFile(ref btnChevronRight, "ChevronRight", PathPluginResources);

            LoadImageFromFile(ref texPanel, "img_PanelBack.png");

            LoadImageFromFile(ref texBarBlue, "img_BarBlue.png");
            LoadImageFromFile(ref texBarBlue_Back, "img_BarBlue_Back.png");
            LoadImageFromFile(ref texBarGreen, "img_BarGreen.png");
            LoadImageFromFile(ref texBarGreen_Back, "img_BarGreen_Back.png");

            LoadImageFromFile(ref texBarHighlight, "img_BarHighlight.png");
            LoadImageFromFile(ref texBarHighlightGreen, "img_BarHighlightGreen.png");
            LoadImageFromFile(ref texBarHighlightRed, "img_BarHighlightRed.png");

            LoadImageFromFile(ref btnChevronUp, "img_buttonChevronUp.png");
            LoadImageFromFile(ref btnChevronDown, "img_buttonChevronDown.png");

            LoadImageFromFile(ref btnViewAll, "img_buttonEye.png");
            LoadImageFromFile(ref btnViewTimes, "img_buttonClock.png");

            LoadImageFromFile(ref btnSettingsAttention, "img_buttonSettingsAttention.png");

            LoadImageFromFile(ref texPartWindowHead, "img_PartWindowHead.png");

            //LoadImageFromFile(ref texTooltipBackground, "tex_TooltipBackground.png");

            LoadImageFromFile(ref texRateUp, "img_RateUp.png");
            LoadImageFromFile(ref texRateDown, "img_RateDown.png");

            LoadImageFromFile(ref btnAlarm, "img_Alarm.png");
            LoadImageFromFile(ref btnAlarmEnabled, "img_AlarmEnabled.png");
            LoadImageFromFile(ref btnAlarmWarn, "img_AlarmWarn.png");
            LoadImageFromFile(ref btnAlarmAlert, "img_AlarmAlert.png");

            //LoadImageFromFile(ref btnLock, "img_Lock.png");
            //LoadImageFromFile(ref btnUnlock, "img_Unlock.png");

            LoadImageFromFile(ref btnDropDown, "img_DropDown.png");
            LoadImageFromFile(ref btnPlay, "img_Play.png");
            LoadImageFromFile(ref btnStop, "img_Stop.png");
            //LoadImageFromFile(ref btnDropDownSep, "img_DropDownSep.png");

            //LoadImageFromFile(ref texDropDownListBox, "tex_DropDownListBox.png");
            //LoadImageFromFile(ref texDropDownListBoxUnity, "tex_DropDownListBoxUnity.png");

            LoadImageFromFile(ref texResourceMove, "img_ResourceMove.png");

            LoadImageFromFile(ref texBox, "tex_Box.png");
            LoadImageFromFile(ref texBoxUnity, "tex_BoxUnity.png");

            LoadImageFromFile(ref texSeparatorH, "img_SeparatorHorizontal.png");
            LoadImageFromFile(ref texSeparatorV, "img_SeparatorVertical.png");

            LoadImageFromFile(ref texAppLaunchIcon, "KSPARPaBig.png", PathPluginToolbarIcons);
        }


        #region Util Stuff
        //internal static Boolean LoadImageFromGameDB(ref Texture2D tex, String FileName, String FolderPath = "")
        //{
        //    Boolean blnReturn = false;
        //    try
        //    {
        //        //trim off the tga and png extensions
        //        if (FileName.ToLower().EndsWith(".png")) FileName = FileName.Substring(0, FileName.Length - 4);
        //        if (FileName.ToLower().EndsWith(".tga")) FileName = FileName.Substring(0, FileName.Length - 4); 
        //        //default folder
        //        if (FolderPath == "") FolderPath = DBPathTextures;

        //        //Look for case mismatches
        //        if (!GameDatabase.Instance.ExistsTexture(String.Format("{0}/{1}", FolderPath, FileName)))
        //            throw new Exception();
                
        //        //now load it
        //        tex = GameDatabase.Instance.GetTexture(String.Format("{0}/{1}", FolderPath, FileName), false);
        //        blnReturn = true;
        //    }
        //    catch (Exception)
        //    {
        //        MonoBehaviourExtended.LogFormatted("Failed to load (are you missing a file - and check case):{0}/{1}", FolderPath, FileName);
        //    }
        //    return blnReturn;
        //}

        /// <summary>
        /// Loads a texture from the file system directly
        /// </summary>
        /// <param name="tex">Unity Texture to Load</param>
        /// <param name="FileName">Image file name</param>
        /// <param name="FolderPath">Optional folder path of image</param>
        /// <returns></returns>
        public static Boolean LoadImageFromFile(ref Texture2D tex, String FileName, String FolderPath = "")
        {
            //DebugLogFormatted("{0},{1}",FileName, FolderPath);
            Boolean blnReturn = false;
            try
            {
                if (FolderPath == "") FolderPath = PathPluginTextures;

                //File Exists check
                if (System.IO.File.Exists(String.Format("{0}/{1}", FolderPath, FileName)))
                {
                    try
                    {
                        MonoBehaviourExtended.LogFormatted_DebugOnly("Loading: {0}", String.Format("{0}/{1}", FolderPath, FileName));
                        tex.LoadImage(System.IO.File.ReadAllBytes(String.Format("{0}/{1}", FolderPath, FileName)));
                        blnReturn = true;
                    }
                    catch (Exception ex)
                    {
                        MonoBehaviourExtended.LogFormatted("Failed to load the texture:{0} ({1})", String.Format("{0}/{1}", FolderPath, FileName), ex.Message);
                    }
                }
                else
                {
                    MonoBehaviourExtended.LogFormatted("Cannot find texture to load:{0}", String.Format("{0}/{1}", FolderPath, FileName));
                }


            }
            catch (Exception ex)
            {
                MonoBehaviourExtended.LogFormatted("Failed to load (are you missing a file):{0} ({1})", String.Format("{0}/{1}", FolderPath, FileName), ex.Message);
            }
            return blnReturn;
        }
        #endregion
    }
}
