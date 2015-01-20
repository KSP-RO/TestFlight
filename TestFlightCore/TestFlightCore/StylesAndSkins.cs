using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;

using KSP;
using UnityEngine;
using KSPPluginFramework;

namespace TestFlightCore
{
    internal class Styles
    {
        #region Styles
        // Base styles
        internal static GUIStyle styleButton;
        internal static GUIStyle styleButtonMain;
        internal static GUIStyle styleButtonSettings;
        internal static GUIStyle styleDropDownButton;
        internal static GUIStyle styleDropDownListItem;
        internal static GUIStyle stylePanel;
        internal static GUIStyle styleText;
        internal static GUIStyle styleTextCenter;
        internal static GUIStyle styleTooltipStyle;
        internal static GUIStyle styleSeparatorV;
        internal static GUIStyle styleSeparatorH;
        internal static GUIStyle styleDropDownListBox;
        internal static GUIStyle styleBarText;
        internal static GUIStyle styleTextCenterGreen;
        internal static GUIStyle styleTextGreen;
        internal static GUIStyle styleTextYellow;
        internal static GUIStyle styleTextYellowBold;
        internal static GUIStyle stylePartWindowPanel;
        internal static GUIStyle styleToggle;
        internal static GUIStyle textStyleSafe;
        internal static GUIStyle textStyleWarning;
        internal static GUIStyle textStyleCritical;
        // KSP Styles
        // Unity Styles
        internal static GUIStyle styleButtonUnity;
        internal static GUIStyle styleButtonMainUnity;
        internal static GUIStyle styleButtonSettingsUnity;
        internal static GUIStyle styleDropDownButtonUnity;
        internal static GUIStyle styleDropDownListBoxUnity;
        // Solarized Dark Styles
        internal static GUIStyle stylePanelSolarizedDark;
        internal static GUIStyle stylePanelSolarizedDarkHUD;
        internal static GUIStyle styleTextSolarizedDark;
        internal static GUIStyle styleButtonSolarizedDark;
        internal static GUIStyle styleTextSafeSolarizedDark;
        internal static GUIStyle styleTextWarningSolarizedDark;
        internal static GUIStyle styleTextCriticalSolarizedDark;
        internal static GUIStyle styleTooltipSolarizedDark;
        internal static GUIStyle styleTooltipRequirementsSolarizedDark;
        // Editor Panels
        internal static GUIStyle styleEditorPanel;
        internal static GUIStyle styleEditorTitle;
        internal static GUIStyle styleEditorText;

        // Solarized Light Styles








        internal static GUIStyle stylePartWindowPanelUnity;
        internal static GUIStyle stylePartWindowHead;
        internal static GUIStyle styleSettingsArea;
        internal static GUIStyle styleDropDownGlyph;
        


        /// <summary>
        /// This one sets up the styles we use
        /// </summary>
        internal static void InitStyles()
        {
            MonoBehaviourExtended.LogFormatted("Configuring Styles");

            #region Styles For Skins
            stylePanel = new GUIStyle();
            stylePanel.normal.background = TestFlight.Resources.texPanel;
            stylePanel.border = new RectOffset(6, 6, 6, 6);
            stylePanel.padding = new RectOffset(8, 3, 7, 0);

            stylePanelSolarizedDark = new GUIStyle();
            stylePanelSolarizedDark.normal.background = TestFlight.Resources.texPanelSolarizedDark;
            stylePanelSolarizedDark.border = new RectOffset(27, 27, 27, 27);
            stylePanelSolarizedDark.padding = new RectOffset(10, 10, 10, 10);

            stylePanelSolarizedDarkHUD = new GUIStyle();
            stylePanelSolarizedDarkHUD.normal.background = CreateColorPixel(new Color32 (0,20,26,200) );


            styleButton = new GUIStyle(SkinsLibrary.DefUnitySkin.button);
            styleButton.name = "ButtonGeneral";
            styleButton.normal.background = SkinsLibrary.DefKSPSkin.button.normal.background;
            styleButton.hover.background = SkinsLibrary.DefKSPSkin.button.hover.background;
            styleButton.normal.textColor = new Color(207, 207, 207);
            styleButton.fontStyle = FontStyle.Normal;
            styleButton.fixedHeight = 20;
            styleButton.padding.top = 2;
            //styleButton.alignment = TextAnchor.MiddleCenter;

            styleButtonUnity = new GUIStyle(styleButton);
            styleButtonUnity.normal.background = SkinsLibrary.DefUnitySkin.button.normal.background;
            styleButtonUnity.hover.background = SkinsLibrary.DefUnitySkin.button.hover.background;


            styleButtonMain = new GUIStyle(styleButton);
            styleButtonMain.name = "ButtonMain";
            styleButtonMain.fixedHeight = 20;

            styleButtonMainUnity = new GUIStyle(styleButtonMain);
            styleButtonMainUnity.normal.background = SkinsLibrary.DefUnitySkin.button.normal.background;
            styleButtonMainUnity.hover.background = SkinsLibrary.DefUnitySkin.button.hover.background;


            styleButtonSettings = new GUIStyle(styleButton);
            styleButtonSettings.name = "ButtonSettings";
            styleButtonSettings.padding = new RectOffset(1, 1, 1, 1);
            //styleButtonSettings.fixedWidth = 40;

            styleButtonSettingsUnity = new GUIStyle(styleButtonSettings);
            styleButtonSettingsUnity.normal.background = SkinsLibrary.DefUnitySkin.button.normal.background;
            styleButtonSettingsUnity.hover.background = SkinsLibrary.DefUnitySkin.button.hover.background;

            styleTooltipStyle = new GUIStyle();
            styleTooltipStyle.name = "Tooltip";
            styleTooltipStyle.fontSize = 11;
            styleTooltipStyle.normal.textColor = new Color32(207, 207, 207, 255);
            styleTooltipStyle.stretchHeight = true;
            styleTooltipStyle.wordWrap = true;
            styleTooltipStyle.normal.background = TestFlight.Resources.texBox;
            //Extra border to prevent bleed of color - actual border is only 1 pixel wide
            styleTooltipStyle.border = new RectOffset(3, 3, 3, 3);
            styleTooltipStyle.padding = new RectOffset(4, 4, 6, 4);
            styleTooltipStyle.alignment = TextAnchor.MiddleCenter;

            styleTooltipSolarizedDark = new GUIStyle(styleTooltipStyle);
            styleTooltipSolarizedDark.fontSize = 12;
            styleTooltipSolarizedDark.normal.background = CreateColorPixel(new Color32(7,54,66,255));
            styleTooltipSolarizedDark.normal.textColor = new Color32(147,161,161,255);

            styleTooltipRequirementsSolarizedDark = new GUIStyle(styleTooltipSolarizedDark);
            styleTooltipRequirementsSolarizedDark.wordWrap = false;
            styleTooltipRequirementsSolarizedDark.alignment = TextAnchor.UpperLeft;
            styleTooltipRequirementsSolarizedDark.richText = true;

            styleDropDownButton = new GUIStyle(styleButton);
            styleDropDownButton.padding.right = 20;
            styleDropDownButtonUnity = new GUIStyle(styleButtonUnity);
            styleDropDownButtonUnity.padding.right = 20;

            styleDropDownListBox = new GUIStyle();
            styleDropDownListBox.normal.background = TestFlight.Resources.texBox;
            //Extra border to prevent bleed of color - actual border is only 1 pixel wide
            styleDropDownListBox.border = new RectOffset(3, 3, 3, 3);

            styleDropDownListBoxUnity = new GUIStyle();
            styleDropDownListBoxUnity.normal.background = CreateColorPixel(new Color32(0,0,0,255));
            //Extra border to prevent bleed of color - actual border is only 1 pixel wide
            styleDropDownListBoxUnity.border = new RectOffset(3, 3, 3, 3);


            styleDropDownListItem = new GUIStyle();
            styleDropDownListItem.normal.textColor = new Color(207, 207, 207);
            Texture2D texBack = Styles.CreateColorPixel(new Color(207, 207, 207));
            styleDropDownListItem.hover.background = texBack;
            styleDropDownListItem.onHover.background = texBack;
            styleDropDownListItem.hover.textColor = Color.black;
            styleDropDownListItem.onHover.textColor = Color.black;
            styleDropDownListItem.padding = new RectOffset(4, 4, 3, 4);


            stylePartWindowPanel = new GUIStyle(stylePanel);
            stylePartWindowPanel.padding = new RectOffset(1, 1, 1, 1);
            stylePartWindowPanel.margin = new RectOffset(0, 0, 0, 0);

            stylePartWindowPanelUnity = new GUIStyle();
            stylePartWindowPanelUnity.padding = new RectOffset(1, 1, 1, 1);
            stylePartWindowPanelUnity.margin = new RectOffset(0, 0, 0, 0);

            stylePartWindowHead = new GUIStyle(GUI.skin.label);
            stylePartWindowHead.normal.background = TestFlight.Resources.texPartWindowHead;
            stylePartWindowHead.border = new RectOffset(6, 6, 6, 6);
            stylePartWindowHead.stretchWidth = true;
            stylePartWindowHead.normal.textColor = Color.white;
            stylePartWindowHead.padding = new RectOffset(5, 1, 1, 1);
            stylePartWindowHead.margin = new RectOffset(0, 0, 0, 0);

            #endregion

            #region Common Styles
            styleText = new GUIStyle(SkinsLibrary.DefUnitySkin.label);
            styleText.fontSize = 11;
            styleText.alignment = TextAnchor.MiddleLeft;
            styleText.normal.textColor = new Color(207, 207, 207);
            styleText.wordWrap = false;
            styleText.richText = true;

            styleTextSolarizedDark = new GUIStyle(styleText);
            styleTextSolarizedDark.fontSize = 11;
            styleTextSolarizedDark.normal.textColor = new Color(131, 148, 150);

            styleTextGreen = new GUIStyle(styleText);
            styleTextGreen.normal.textColor = new Color32(183, 254, 0, 255); ;
            styleTextYellow = new GUIStyle(styleText);
            styleTextYellow.normal.textColor = Color.yellow;
            styleTextYellowBold = new GUIStyle(styleTextYellow);
            styleTextYellowBold.fontStyle = FontStyle.Bold;

            styleTextCenter = new GUIStyle(styleText);
            styleTextCenter.alignment = TextAnchor.MiddleCenter;
            styleTextCenterGreen = new GUIStyle(styleTextCenter);
            styleTextCenterGreen.normal.textColor = new Color32(183, 254, 0, 255);
            
            styleBarText = new GUIStyle(styleText);
            styleBarText.alignment = TextAnchor.MiddleCenter;
            styleBarText.normal.textColor = new Color(255, 255, 255, 0.8f);

            textStyleSafe = new GUIStyle(styleText);
            textStyleSafe.normal.textColor = new Color32(133, 153, 0, 255);        

            styleTextSafeSolarizedDark = new GUIStyle(textStyleSafe);
            styleTextSafeSolarizedDark.fontSize = 11;

            textStyleWarning = new GUIStyle(styleText);
            textStyleWarning.normal.textColor = new Color32(203, 75, 22, 255);        

            styleTextWarningSolarizedDark = new GUIStyle(textStyleWarning);
            styleTextWarningSolarizedDark.fontSize = 11;

            textStyleCritical = new GUIStyle(styleText);
            textStyleCritical.normal.textColor = new Color32(220, 50, 47, 255);        

            styleTextCriticalSolarizedDark = new GUIStyle(textStyleCritical);
            styleTextCriticalSolarizedDark.fontSize = 11;

            styleToggle = new GUIStyle(HighLogic.Skin.toggle);
            styleToggle.normal.textColor = new Color(207, 207, 207);
            styleToggle.fixedHeight = 20;
            styleToggle.padding = new RectOffset(6, 0, -2, 0);

            styleSettingsArea = new GUIStyle(HighLogic.Skin.textArea);
            styleSettingsArea.padding = new RectOffset(0, 0, 0, 4);

            styleDropDownGlyph = new GUIStyle();
            styleDropDownGlyph.alignment = TextAnchor.MiddleCenter;
            
            styleSeparatorV = new GUIStyle();
            styleSeparatorV.normal.background = TestFlight.Resources.texSeparatorV;
            styleSeparatorV.border = new RectOffset(0, 0, 6, 6);
            styleSeparatorV.fixedWidth = 2;

            styleSeparatorH = new GUIStyle();
            styleSeparatorH.normal.background = TestFlight.Resources.texSeparatorH;
            styleSeparatorH.border = new RectOffset(6, 6, 0, 0);
            styleSeparatorH.fixedHeight = 2;

            #endregion

            #region Editor Styles
            styleEditorPanel = new GUIStyle();
            styleEditorPanel.normal.background = CreateColorPixel(new Color32(0,0,0,128));
            styleEditorPanel.padding = new RectOffset(5, 5, 3, 3);

            styleEditorTitle = new GUIStyle();
            styleEditorTitle.normal.textColor = Color.yellow;
            styleEditorTitle.fontSize = 11;
            styleEditorTitle.fontStyle = FontStyle.Bold;

            styleEditorText = new GUIStyle();
            styleEditorText.normal.textColor = Color.white;
            styleEditorText.fontSize = 11;
            styleEditorText.fontStyle = FontStyle.Normal;
            styleEditorText.richText = true;
            #endregion
        }
        #endregion


        /// <summary>
        /// This one creates the skins, adds em to the skins library and adds needed styles
        /// </summary>
        internal static void InitSkins()
        {
            //Default Skin
            GUISkin DefKSP = SkinsLibrary.CopySkin(SkinsLibrary.DefSkinType.KSP);
            DefKSP.window = stylePanel;
            DefKSP.font = SkinsLibrary.DefUnitySkin.font;
            DefKSP.horizontalSlider.margin.top = 8;
            SkinsLibrary.AddSkin("Default", DefKSP);

            //Adjust Default Skins
            SkinsLibrary.List["Default"].button = new GUIStyle(styleButton);
            SkinsLibrary.List["Default"].label = new GUIStyle(styleText);

            //Add Styles once skin is added
            SkinsLibrary.AddStyle("Default", styleTooltipStyle);
            SkinsLibrary.AddStyle("Default", styleButton);
            SkinsLibrary.AddStyle("Default", styleButtonMain);
            SkinsLibrary.AddStyle("Default", styleButtonSettings);
            SkinsLibrary.AddStyle("Default", "DropDownButton", styleDropDownButton);
            SkinsLibrary.AddStyle("Default", "DropDownListBox", styleDropDownListBox);
            SkinsLibrary.AddStyle("Default", "DropDownListItem", styleDropDownListItem);

            SkinsLibrary.AddStyle("Default", "SafeText", textStyleSafe);
            SkinsLibrary.AddStyle("Default", "WarningText", textStyleWarning);
            SkinsLibrary.AddStyle("Default", "CriticalText", textStyleCritical);


            //Now a Unity Style one
            GUISkin DefUnity = SkinsLibrary.CopySkin(SkinsLibrary.DefSkinType.Unity);
            DefUnity.window = DefUnity.box;
            DefUnity.window.border = new RectOffset(6, 6, 6, 6);
            DefUnity.window.padding = new RectOffset(8, 3, 7, 0);
            DefUnity.horizontalSlider.margin.top = 8;
            SkinsLibrary.AddSkin("Unity", DefUnity);

            //Adjust Default Skins
            SkinsLibrary.List["Unity"].button = new GUIStyle(styleButtonUnity);
            SkinsLibrary.List["Unity"].label = new GUIStyle(styleText);

            //Add Styles once skin is added
            GUIStyle styleTooltipUnity = new GUIStyle(styleTooltipStyle);
            styleTooltipUnity.normal.background = GUI.skin.box.normal.background;
            styleTooltipUnity.normal.textColor = Color.white;
            SkinsLibrary.AddStyle("Unity", styleTooltipUnity);
            SkinsLibrary.AddStyle("Unity", styleButtonUnity);
            SkinsLibrary.AddStyle("Unity", styleButtonMainUnity);
            SkinsLibrary.AddStyle("Unity", styleButtonSettingsUnity);
            SkinsLibrary.AddStyle("Unity", "DropDownButton", styleDropDownButtonUnity);
            SkinsLibrary.AddStyle("Unity", "DropDownListBox", styleDropDownListBoxUnity);
            SkinsLibrary.AddStyle("Unity", "DropDownListItem", styleDropDownListItem);

            SkinsLibrary.AddStyle("Unity", "SafeText", textStyleSafe);
            SkinsLibrary.AddStyle("Unity", "WarningText", textStyleWarning);
            SkinsLibrary.AddStyle("Unity", "CriticalText", textStyleCritical);

            // Solarized Dark theme
            // http://ethanschoonover.com/solarized
            GUISkin solarizedDarkSkin = SkinsLibrary.CopySkin("Unity");
            solarizedDarkSkin.window = stylePanelSolarizedDark;
            solarizedDarkSkin.label.fontSize = 11;
            SkinsLibrary.AddSkin("SolarizedDark", solarizedDarkSkin);
            SkinsLibrary.AddStyle("SolarizedDark", "HUD", stylePanelSolarizedDarkHUD);
            SkinsLibrary.AddStyle("SolarizedDark", "Tooltip", styleTooltipRequirementsSolarizedDark);
            SkinsLibrary.AddStyle("SolarizedDark", "DropDownButton", styleDropDownButtonUnity);
            SkinsLibrary.AddStyle("SolarizedDark", "DropDownListBox", styleDropDownListBoxUnity);
            SkinsLibrary.AddStyle("SolarizedDark", "DropDownListItem", styleDropDownListItem);

            // Editor window is based on Kerbal Engineer's editor window to provide a more harmonious looks (because *I* use KER)
            GUISkin testFlightEditor = SkinsLibrary.CopySkin("Unity");
            testFlightEditor.window = styleEditorPanel;
            testFlightEditor.label.fontSize = 11;
            testFlightEditor.label = styleEditorText;
            SkinsLibrary.AddSkin("TestFlightEditor", testFlightEditor);
        }

        /// <summary>
        /// Creates a 1x1 texture
        /// </summary>
        /// <param name="Background">Color of the texture</param>
        /// <returns></returns>
        internal static Texture2D CreateColorPixel(Color32 Background)
        {
            Texture2D retTex = new Texture2D(1, 1);
            retTex.SetPixel(0, 0, Background);
            retTex.Apply();
            return retTex;
        }
    }
}
