using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;

using KSP;
using UnityEngine;
using TestFlightCore.KSPPluginFramework;

namespace TestFlight
{
    internal class Resources
    {
        //WHERE SHOULD THESE BE???
        internal static String PathApp = KSPUtil.ApplicationRootPath.Replace("\\", "/");
        internal static String PathTestFlight = string.Format("{0}GameData/TestFlight", PathApp);
        internal static String PathPlugin = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        internal static String PathPluginResources = string.Format("{0}/Resources", PathTestFlight);
        internal static String PathPluginTextures = string.Format("{0}/Resources/Textures", PathTestFlight);

        internal static Texture2D texPanel;
        internal static Texture2D texPanelSolarizedDark;

        internal static Texture2D btnChevronUp;
        internal static Texture2D btnChevronDown;

        internal static Texture2D texPartWindowHead;

        internal static Texture2D texBox;
        internal static Texture2D texBoxUnity;

        internal static Texture2D texSeparatorV;
        internal static Texture2D texSeparatorH;

        internal static void LoadTextures()
        {
            MonoBehaviourExtended.LogFormatted("Loading Textures");

            if (btnChevronUp == null)
            {
                btnChevronUp = new Texture2D(17, 16, TextureFormat.ARGB32, false);
                LoadImageFromFile(ref btnChevronUp, "ChevronUp.png", PathPluginResources);
            }

            if (btnChevronDown == null)
            {
                btnChevronDown = new Texture2D(17, 16, TextureFormat.ARGB32, false);
                LoadImageFromFile(ref btnChevronDown, "ChevronDown.png", PathPluginResources);
            }

            if (texPanel == null)
            {
                texPanel = new Texture2D(16, 16, TextureFormat.ARGB32, false);
                LoadImageFromFile(ref texPanel, "img_PanelBack.png");
            }

            if (texPanelSolarizedDark == null)
            {
                texPanelSolarizedDark = new Texture2D(16, 16, TextureFormat.ARGB32, false);
                LoadImageFromFile(ref texPanelSolarizedDark, "img_PanelSolarizedDark.png");
            }

            if (texPartWindowHead == null)
            {
                texPartWindowHead = new Texture2D(16, 16, TextureFormat.ARGB32, false);
                LoadImageFromFile(ref texPartWindowHead, "img_PartWindowHead.png");
            }

            if (texBox == null)
            {
                texBox = new Texture2D(9, 9, TextureFormat.ARGB32, false);
                LoadImageFromFile(ref texBox, "tex_Box.png");
            }

            if (texBoxUnity == null)
            {
                texBoxUnity = new Texture2D(9, 9, TextureFormat.ARGB32, false);
                LoadImageFromFile(ref texBoxUnity, "tex_BoxUnity.png");
            }

            if (texSeparatorH == null)
            {
                texSeparatorH = new Texture2D(2, 20, TextureFormat.ARGB32, false);
                LoadImageFromFile(ref texSeparatorH, "img_SeparatorHorizontal.png");
            }

            if (texSeparatorV == null)
            {
                texSeparatorV = new Texture2D(6, 2, TextureFormat.ARGB32, false);
                LoadImageFromFile(ref texSeparatorV, "img_SeparatorVertical.png");
            }
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
                if (File.Exists(String.Format("{0}/{1}", FolderPath, FileName)))
                {
                    try
                    {
                        MonoBehaviourExtended.LogFormatted_DebugOnly("Loading: {0}", String.Format("{0}/{1}", FolderPath, FileName));
                        tex.LoadImage(File.ReadAllBytes(String.Format("{0}/{1}", FolderPath, FileName)));
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
