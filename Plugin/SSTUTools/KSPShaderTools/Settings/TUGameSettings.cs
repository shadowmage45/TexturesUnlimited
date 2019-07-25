using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KSPShaderTools.Settings
{

    public class TUGameSettings : GameParameters.CustomParameterNode
    {

        [GameParameters.CustomParameterUI(
            "Custom Editor Reflections",
            gameMode = GameParameters.GameMode.ANY,
            newGameOnly = false,
            unlockedDuringMission = false,
            toolTip = "Should the pre-baked stock editor reflection maps be replaced with runtime baked maps that used proper specular convolution?  Makes decreases reflections in the editor, but they will now match how they will look in the flight scene.")]
        public bool customEditorReflections = true;

        [GameParameters.CustomIntParameterUI(
            "Reflection Resolution",
            gameMode = GameParameters.GameMode.ANY,
            newGameOnly = false,
            unlockedDuringMission = false,
            minValue = 1,
            maxValue = 6,
            stepSize = 1,
            toolTip = "Reflection texture size. Larger values give better looking reflections at an increased cost to update and render.  Use lower settings for better performance.  Map Resolution. 1=64x, 2=128x, 3=256x, 4=512x, 5=1024x, 6=2048x")]
        public int reflectionResolution = 4;

        [GameParameters.CustomIntParameterUI(
            "Recolor GUI Width",
            gameMode = GameParameters.GameMode.ANY,
            newGameOnly = false,
            unlockedDuringMission = false,
            minValue = 100,
            maxValue = 2000,
            stepSize = 10,
            toolTip = "Width of the recoloring GUI in pixels.")]
        public int recolorGUIWidth = 400;

        [GameParameters.CustomIntParameterUI(
            "Recolor GUI Height",
            gameMode = GameParameters.GameMode.ANY,
            newGameOnly = false,
            unlockedDuringMission = false,
            minValue = 100,
            maxValue = 2000,
            stepSize = 10,
            toolTip = "Height of the recoloring GUI in pixels.")]
        public int recolorGUIHeight = 540;

        [GameParameters.CustomIntParameterUI(
            "Recolor GUI Top Height",
            gameMode = GameParameters.GameMode.ANY,
            newGameOnly = false,
            unlockedDuringMission = false,
            minValue = 100,
            maxValue = 2000,
            stepSize = 10,
            toolTip = "Height of the top section of the recoloring GUI in pixels.")]
        public int recolorGUITopSectionHeight = 100;

        public override string Title => "Textures Unlimited";

        public override string DisplaySection => "TU";

        public override string Section => "TU";

        public override int SectionOrder => 1;

        public override GameParameters.GameMode GameMode => GameParameters.GameMode.ANY;

        public override bool HasPresets => false;

        public static bool CustomEditorReflections => HighLogic.CurrentGame.Parameters.CustomParams<TUGameSettings>().customEditorReflections;

        public static int ReflectionResolution
        {
            get
            {
                int raw = HighLogic.CurrentGame.Parameters.CustomParams<TUGameSettings>().reflectionResolution;
                switch (raw)
                {
                    case 1:
                        return 64;
                    case 2:
                        return 128;
                    case 3:
                        return 256;
                    case 4:
                        return 512;
                    case 5:
                        return 1024;
                    case 6:
                        return 2048;
                    default:
                        return 512;
                }
            }
        }

        public static int RecolorGUIWidth => HighLogic.CurrentGame.Parameters.CustomParams<TUGameSettings>().recolorGUIWidth;
        public static int RecolorGUIHeight => HighLogic.CurrentGame.Parameters.CustomParams<TUGameSettings>().recolorGUIHeight;
        public static int RecolorGUITopHeight => HighLogic.CurrentGame.Parameters.CustomParams<TUGameSettings>().recolorGUITopSectionHeight;

    }

}
