using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;
using UnityEngine;
using System.IO;

namespace KSPShaderTools
{
    public static class Utils
    {

        #region parsing methods

        public static double safeParseDouble(String val)
        {
            double returnVal = 0;
            if (double.TryParse(val, out returnVal))
            {
                return returnVal;
            }
            return 0;
        }

        internal static bool safeParseBool(string v)
        {
            if (v == null) { return false; }
            else if (v.Equals("true") || v.Equals("yes") || v.Equals("1")) { return true; }
            return false;
        }

        public static float safeParseFloat(String val)
        {
            float returnVal = 0;
            try
            {
                returnVal = float.Parse(val);
            }
            catch (Exception e)
            {
                MonoBehaviour.print("ERROR: could not parse float value from: '" + val + "'\n" + e.Message);
            }
            return returnVal;
        }

        public static int safeParseInt(String val)
        {
            int returnVal = 0;
            try
            {
                returnVal = int.Parse(val);
            }
            catch (Exception e)
            {
                MonoBehaviour.print("ERROR: could not parse int value from: '" + val + "'\n" + e.Message);
            }
            return returnVal;
        }

        public static String[] parseCSV(String input)
        {
            return parseCSV(input, ",");
        }

        public static String[] parseCSV(String input, String split)
        {
            String[] vals = input.Split(new String[] { split }, StringSplitOptions.None);
            int len = vals.Length;
            for (int i = 0; i < len; i++)
            {
                vals[i] = vals[i].Trim();
            }
            return vals;
        }

        public static float[] parseFloatArray(string input)
        {
            string[] strs = parseCSV(input);
            int len = strs.Length;
            float[] flts = new float[len];
            for (int i = 0; i < len; i++)
            {
                flts[i] = safeParseFloat(strs[i]);
            }
            return flts;
        }

        public static Color parseColorFromBytes(string input)
        {
            Color color = new Color();
            float[] vals = parseFloatArray(input);
            color.r = vals[0] / 255f;
            color.g = vals[1] / 255f;
            color.b = vals[2] / 255f;
            if (vals.Length > 3)
            {
                color.a = vals[3] / 255f;
            }            
            return color;
        }

        public static Color parseColorFromFloats(string input)
        {
            input = input.Replace("(", "");
            input = input.Replace(")", "");
            Color color = new Color();
            float[] vals = parseFloatArray(input);
            color.r = vals[0];
            color.g = vals[1];
            color.b = vals[2];
            if (vals.Length > 3)
            {
                color.a = vals[3];
            }            
            return color;
        }

        /// <summary>
        /// Will parse a value from the input formats of:<para/>
        /// 255,255,255,255 (byte notation)<para/>
        /// 1.0,1.0,1.0,1.0 (float notation)<para/>
        /// #FFFFFFFF (hex notation)<para/>
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static Color parseColor(string input)
        {
            Color color = Color.white;
            input = input.Trim();
            //check for occurance of ','
            //if present, it is in CSV notation and assume it is either float or byte notation
            if (input.Contains(","))
            {
                //if it contains periods, parse as float values
                if (input.Contains("."))
                {
                    return parseColorFromFloats(input);
                }
                else //else assume it is byte notation, parse as byte values
                {
                    return parseColorFromBytes(input);
                }
            }
            else if (input.Contains("#"))
            {
                //check length -- if it is 7 chars it should be RGB, 9 chars = RGBA
                if (!ColorUtility.TryParseHtmlString(input, out color))
                {
                    MonoBehaviour.print("ERROR: Could not parse HTML color value from the input string of: " + input);
                }
            }
            else
            {
                MonoBehaviour.print("ERROR: Could not determine color format from input: "+input+" , returning Color.white");
            }
            return color;
        }

        public static ConfigNode parseConfigNode(String input)
        {
            ConfigNode baseCfn = ConfigNode.Parse(input);
            if (baseCfn == null) { MonoBehaviour.print("ERROR: Base config node was null!!\n" + input); }
            else if (baseCfn.nodes.Count <= 0) { MonoBehaviour.print("ERROR: Base config node has no nodes!!\n" + input); }
            return baseCfn.nodes[0];
        }

        #endregion

        #region ConfigNode extension methods

        public static String[] GetStringValues(this ConfigNode node, String name, bool reverse = false)
        {
            string[] values = node.GetValues(name);
            if (reverse)
            {
                int len = values.Length;
                string[] returnValues = new string[len];
                for (int i = 0, k = len - 1; i < len; i++, k--)
                {
                    returnValues[i] = values[k];
                }
                values = returnValues;
            }
            return values;
        }

        public static string[] GetStringValues(this ConfigNode node, string name, string[] defaults, bool reverse = false)
        {
            if (node.HasValue(name)) { return node.GetStringValues(name, reverse); }
            return defaults;
        }

        public static string GetStringValue(this ConfigNode node, String name, String defaultValue)
        {
            String value = node.GetValue(name);
            return value == null ? defaultValue : value;
        }

        public static string GetStringValue(this ConfigNode node, String name)
        {
            return GetStringValue(node, name, "");
        }

        public static bool[] GetBoolValues(this ConfigNode node, String name)
        {
            String[] values = node.GetValues(name);
            int len = values.Length;
            bool[] vals = new bool[len];
            for (int i = 0; i < len; i++)
            {
                vals[i] = Utils.safeParseBool(values[i]);
            }
            return vals;
        }

        public static bool GetBoolValue(this ConfigNode node, String name, bool defaultValue)
        {
            String value = node.GetValue(name);
            if (value == null) { return defaultValue; }
            try
            {
                return bool.Parse(value);
            }
            catch (Exception e)
            {
                MonoBehaviour.print(e.Message);
            }
            return defaultValue;
        }

        public static bool GetBoolValue(this ConfigNode node, String name)
        {
            return GetBoolValue(node, name, false);
        }

        public static float[] GetFloatValues(this ConfigNode node, String name, float[] defaults)
        {
            String baseVal = node.GetStringValue(name);
            if (!String.IsNullOrEmpty(baseVal))
            {
                String[] split = baseVal.Split(new char[] { ',' });
                float[] vals = new float[split.Length];
                for (int i = 0; i < split.Length; i++) { vals[i] = Utils.safeParseFloat(split[i]); }
                return vals;
            }
            return defaults;
        }

        public static float[] GetFloatValues(this ConfigNode node, String name)
        {
            return GetFloatValues(node, name, new float[] { });
        }

        public static float[] GetFloatValuesCSV(this ConfigNode node, String name)
        {
            return GetFloatValuesCSV(node, name, new float[] { });
        }

        public static float[] GetFloatValuesCSV(this ConfigNode node, String name, float[] defaults)
        {
            float[] values = defaults;
            if (node.HasValue(name))
            {
                string strVal = node.GetStringValue(name);
                string[] splits = strVal.Split(',');
                values = new float[splits.Length];
                for (int i = 0; i < splits.Length; i++)
                {
                    values[i] = float.Parse(splits[i]);
                }
            }
            return values;
        }

        public static float GetFloatValue(this ConfigNode node, String name, float defaultValue)
        {
            String value = node.GetValue(name);
            if (value == null) { return defaultValue; }
            try
            {
                return float.Parse(value);
            }
            catch (Exception e)
            {
                MonoBehaviour.print(e.Message);
            }
            return defaultValue;
        }

        public static float GetFloatValue(this ConfigNode node, String name)
        {
            return GetFloatValue(node, name, 0);
        }

        public static double GetDoubleValue(this ConfigNode node, String name, double defaultValue)
        {
            String value = node.GetValue(name);
            if (value == null) { return defaultValue; }
            try
            {
                return double.Parse(value);
            }
            catch (Exception e)
            {
                MonoBehaviour.print(e.Message);
            }
            return defaultValue;
        }

        public static double GetDoubleValue(this ConfigNode node, String name)
        {
            return GetDoubleValue(node, name, 0);
        }

        public static int GetIntValue(this ConfigNode node, String name, int defaultValue)
        {
            String value = node.GetValue(name);
            if (value == null) { return defaultValue; }
            try
            {
                return int.Parse(value);
            }
            catch (Exception e)
            {
                MonoBehaviour.print(e.Message);
            }
            return defaultValue;
        }

        public static int GetIntValue(this ConfigNode node, String name)
        {
            return GetIntValue(node, name, 0);
        }

        public static int[] GetIntValues(this ConfigNode node, string name, int[] defaultValues = null)
        {
            int[] values = defaultValues;
            string[] stringValues = node.GetValues(name);
            if (stringValues == null || stringValues.Length == 0) { return values; }
            int len = stringValues.Length;
            values = new int[len];
            for (int i = 0; i < len; i++)
            {
                values[i] = Utils.safeParseInt(stringValues[i]);
            }
            return values;
        }

        public static Vector4 GetVector4(this ConfigNode node, string name)
        {
            String value = node.GetValue(name);
            if (value == null)
            {
                return Vector4.zero;
            }
            String[] vals = value.Split(',');
            float x = 0, y = 0, z = 0, w = 0;
            if (vals.Length > 0) { x = Utils.safeParseFloat(vals[0].Trim()); }
            if (vals.Length > 1) { y = Utils.safeParseFloat(vals[1].Trim()); }
            if (vals.Length > 2) { z = Utils.safeParseFloat(vals[2].Trim()); }
            if (vals.Length > 3) { w = Utils.safeParseFloat(vals[3].Trim()); }
            return new Vector4(x,y,z,w);
        }

        public static Vector3 GetVector3(this ConfigNode node, String name, Vector3 defaultValue)
        {
            String value = node.GetValue(name);
            if (value == null)
            {
                return defaultValue;
            }
            String[] vals = value.Split(',');
            float x = 0, y = 0, z = 0;
            if (vals.Length > 0) { x = Utils.safeParseFloat(vals[0].Trim()); }
            if (vals.Length > 1) { y = Utils.safeParseFloat(vals[1].Trim()); }
            if (vals.Length > 2) { z = Utils.safeParseFloat(vals[2].Trim()); }
            return new Vector3(x, y, z);
        }

        public static Vector3 GetVector3(this ConfigNode node, String name)
        {
            String value = node.GetValue(name);
            if (value == null)
            {
                MonoBehaviour.print("ERROR: No value for name: " + name + " found in config node: " + node);
                return Vector3.zero;
            }
            String[] vals = value.Split(',');
            float x = 0, y = 0, z = 0;
            if (vals.Length > 0) { x = Utils.safeParseFloat(vals[0].Trim()); }
            if (vals.Length > 1) { y = Utils.safeParseFloat(vals[1].Trim()); }
            if (vals.Length > 2) { z = Utils.safeParseFloat(vals[2].Trim()); }
            return new Vector3(x, y, z);
        }

        public static Vector2 GetVector2(this ConfigNode node, string name, Vector2 defaultValue)
        {
            string value = node.GetValue(name);
            if (string.IsNullOrEmpty (value))
            {
                //MonoBehaviour.print("ERROR parsing values for Vector2 from input: " + value + ". found less than 2 values, cannot create Vector2");
                return defaultValue;
            }
            string[] vals = value.Split(',');
            if (vals.Length < 2)
            {
                MonoBehaviour.print("ERROR parsing values for Vector2 from input: " + value + ". found less than 2 values, cannot create Vector2");
                return defaultValue;
            }
            float a, b;
            a = safeParseFloat(vals[0]);
            b = safeParseFloat(vals[1]);
            return new Vector2(a, b);
        }

        public static Vector2 GetVector2(this ConfigNode node, string name)
        {
            return node.GetVector2(name, Vector2.zero);
        }

        public static FloatCurve GetFloatCurve(this ConfigNode node, String name, FloatCurve defaultValue = null)
        {
            FloatCurve curve = new FloatCurve();
            if (node.HasNode(name))
            {
                ConfigNode curveNode = node.GetNode(name);
                String[] values = curveNode.GetValues("key");
                int len = values.Length;
                String[] splitValue;
                float a, b, c, d;
                for (int i = 0; i < len; i++)
                {
                    splitValue = Regex.Replace(values[i], @"\s+", " ").Split(' ');
                    if (splitValue.Length > 2)
                    {
                        a = Utils.safeParseFloat(splitValue[0]);
                        b = Utils.safeParseFloat(splitValue[1]);
                        c = Utils.safeParseFloat(splitValue[2]);
                        d = Utils.safeParseFloat(splitValue[3]);
                        curve.Add(a, b, c, d);
                    }
                    else
                    {
                        a = Utils.safeParseFloat(splitValue[0]);
                        b = Utils.safeParseFloat(splitValue[1]);
                        curve.Add(a, b);
                    }
                }
            }
            else if (defaultValue != null)
            {
                foreach (Keyframe f in defaultValue.Curve.keys)
                {
                    curve.Add(f.time, f.value, f.inTangent, f.outTangent);
                }
            }
            else
            {
                curve.Add(0, 0);
                curve.Add(1, 1);
            }
            return curve;
        }

        public static Color GetColorFromFloatCSV(this ConfigNode node, string name)
        {
            return parseColorFromFloats(node.GetStringValue(name));
        }

        public static Color GetColorFromByteCSV(this ConfigNode node, string name)
        {
            return parseColorFromBytes(node.GetStringValue(name));
        }

        public static Color GetColor(this ConfigNode node, string name)
        {
            return parseColor(node.GetStringValue(name));
        }

        /// <summary>
        /// Returns a floating point color channel value
        /// </summary>
        /// <param name="node"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static float GetColorChannelValue(this ConfigNode node, string name)
        {
            string value = node.GetStringValue(name).Trim();
            float floatValue = safeParseFloat(value);
            if (value.Contains(".")) { return floatValue; }
            return floatValue / 255f;
        }

        public static string ToStringFixedOrder(this ConfigNode node)
        {
            return ToStringRecurse(node, 0, new StringBuilder()).ToString();
        }

        /// <summary>
        /// Returns the builder instance for....reasons....
        /// </summary>
        /// <param name="node"></param>
        /// <param name="indent"></param>
        /// <param name="builder"></param>
        /// <returns></returns>
        private static StringBuilder ToStringRecurse(this ConfigNode node, int indent, StringBuilder builder)
        {
            if (builder == null)
            {
                builder = new StringBuilder();
            }
            //node title
            builder.Append(' ', indent);
            builder.Append(node.id);
            builder.Append('\n');

            //open brace for this node
            builder.Append(' ', indent);
            builder.Append('{');
            builder.Append('\n');

            //add values
            int len = node.CountValues;
            for (int i = 0; i < len; i++)
            {

                builder.Append(node.values[i].name);
                builder.Append('=');
                builder.Append(node.values[i].value);
                builder.Append('\n');
            }

            //add sub-nodes, increasing the indent count, and adding a newline after every one
            len = node.CountNodes;
            for (int i = 0; i < len; i++)
            {
                ConfigNode node1 = node.nodes[i];
                ToStringRecurse(node1, indent + 2, builder);
                builder.Append('\n');
            }

            //closing brace for this node
            builder.Append(' ', indent);
            builder.Append('}');
            return builder;
        }

        #endregion

        #region Transform extensionMethods

        /// <summary>
        /// Same as transform.FindChildren() but also searches for children with the (Clone) tag on the name.
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="modelName"></param>
        /// <returns></returns>
        public static Transform[] FindModels(this Transform transform, String modelName)
        {
            Transform[] trs = transform.FindChildren(modelName);
            Transform[] trs2 = transform.FindChildren(modelName + "(Clone)");
            Transform[] trs3 = new Transform[trs.Length + trs2.Length];
            int index = 0;
            for (int i = 0; i < trs.Length; i++, index++)
            {
                trs3[index] = trs[i];
            }
            for (int i = 0; i < trs2.Length; i++, index++)
            {
                trs3[index] = trs2[i];
            }
            return trs3;
        }

        /// <summary>
        /// Same as transform.FindRecursive() but also searches for models with "(Clone)" added to the end of the transform name
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="modelName"></param>
        /// <returns></returns>
        public static Transform FindModel(this Transform transform, String modelName)
        {
            Transform tr = transform.FindRecursive(modelName);
            if (tr != null) { return tr; }
            return transform.FindRecursive(modelName + "(Clone)");
        }

        /// <summary>
        /// Same as transform.FindRecursive() but returns an array of all children with that name under the entire heirarchy of the model
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Transform[] FindChildren(this Transform transform, String name)
        {
            List<Transform> trs = new List<Transform>();
            if (transform.name == name) { trs.Add(transform); }
            locateTransformsRecursive(transform, name, trs);
            return trs.ToArray();
        }

        private static void locateTransformsRecursive(Transform tr, String name, List<Transform> output)
        {
            foreach (Transform child in tr)
            {
                if (child.name == name) { output.Add(child); }
                locateTransformsRecursive(child, name, output);
            }
        }

        /// <summary>
        /// Searches entire model heirarchy from the input transform to end of branches for transforms with the input transform name and returns the first match found, or null if none.
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Transform FindRecursive(this Transform transform, String name)
        {
            if (transform.name == name) { return transform; }//was the original input transform
            Transform tr = transform.Find(name);//found as a direct child
            if (tr != null) { return tr; }
            foreach (Transform child in transform)
            {
                tr = child.FindRecursive(name);
                if (tr != null) { return tr; }
            }
            return null;
        }

        /// <summary>
        /// Uses transform.FindRecursive to search for the given transform as a child of the input transform; if it does not exist, it creates a new transform and nests it to the input transform (0,0,0 local position and scale).
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Transform FindOrCreate(this Transform transform, String name)
        {
            Transform newTr = transform.FindRecursive(name);
            if (newTr != null)
            {
                return newTr;
            }
            GameObject newGO = new GameObject(name);
            newGO.SetActive(true);
            newGO.name = newGO.transform.name = name;
            newGO.transform.NestToParent(transform);
            return newGO.transform;
        }

        /// <summary>
        /// Returns -ALL- children/grand-children/etc transforms of the input; everything in the heirarchy.
        /// </summary>
        /// <param name="transform"></param>
        /// <returns></returns>
        public static Transform[] GetAllChildren(this Transform transform)
        {
            List<Transform> trs = new List<Transform>();
            recurseAddChildren(transform, trs);
            return trs.ToArray();
        }

        private static void recurseAddChildren(Transform transform, List<Transform> trs)
        {
            int len = transform.childCount;
            foreach (Transform child in transform)
            {
                trs.Add(child);
                recurseAddChildren(child, trs);
            }
        }

        /// <summary>
        /// Returns true if the input 'isParent' transform exists anywhere upwards of the input transform in the heirarchy.
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="isParent"></param>
        /// <param name="checkUpwards"></param>
        /// <returns></returns>
        public static bool isParent(this Transform transform, Transform isParent, bool checkUpwards = true)
        {
            if (isParent == null) { return false; }
            if (isParent == transform.parent) { return true; }
            if (checkUpwards)
            {
                Transform p = transform.parent;
                if (p == null) { return false; }
                else { p = p.parent; }
                while (p != null)
                {
                    if (p == isParent) { return true; }
                    p = p.parent;
                }
            }
            return false;
        }

        public static void recursePrintComponents(GameObject go, String prefix)
        {
            int childCount = go.transform.childCount;
            Component[] comps = go.GetComponents<Component>();
            MonoBehaviour.print("Found gameObject: " + prefix + go.name + " enabled: " + go.activeSelf + " inHierarchy: " + go.activeInHierarchy + " layer: " + go.layer + " children: " + childCount + " components: " + comps.Length + " position: " + go.transform.position + " scale: " + go.transform.localScale);
            foreach (Component comp in comps)
            {
                if (comp is MeshRenderer)
                {
                    MeshRenderer r = (MeshRenderer)comp;
                    Material m = r.material;
                    Shader s = m == null ? null : m.shader;
                    MonoBehaviour.print("Found Mesh Renderer component.  Mat/shader: " + m + " : " + s);
                }
                else
                {
                    MonoBehaviour.print("Found Component : " + prefix + "* " + comp);
                }
            }
            Transform t = go.transform;
            foreach (Transform child in t)
            {
                recursePrintComponents(child.gameObject, prefix + "  ");
            }
        }

        #endregion

        #region PartModule extensionMethods

        public static void updateUIFloatEditControl(this PartModule module, string fieldName, float min, float max, float incLarge, float incSmall, float incSlide, bool forceUpdate, float forceVal)
        {
            UI_FloatEdit widget = null;
            if (HighLogic.LoadedSceneIsEditor)
            {
                widget = (UI_FloatEdit)module.Fields[fieldName].uiControlEditor;
            }
            else if (HighLogic.LoadedSceneIsFlight)
            {
                widget = (UI_FloatEdit)module.Fields[fieldName].uiControlFlight;
            }
            else
            {
                return;
            }
            if (widget == null)
            {
                return;
            }
            widget.minValue = min;
            widget.maxValue = max;
            widget.incrementLarge = incLarge;
            widget.incrementSmall = incSmall;
            widget.incrementSlide = incSlide;
            if (forceUpdate && widget.partActionItem != null)
            {
                UIPartActionFloatEdit ctr = (UIPartActionFloatEdit)widget.partActionItem;
                var t = widget.onFieldChanged;//temporarily remove the callback
                widget.onFieldChanged = null;
                ctr.incSmall.onToggle.RemoveAllListeners();
                ctr.incLarge.onToggle.RemoveAllListeners();
                ctr.decSmall.onToggle.RemoveAllListeners();
                ctr.decLarge.onToggle.RemoveAllListeners();
                ctr.slider.onValueChanged.RemoveAllListeners();
                ctr.Setup(ctr.Window, module.part, module, HighLogic.LoadedSceneIsEditor ? UI_Scene.Editor : UI_Scene.Flight, widget, module.Fields[fieldName]);
                widget.onFieldChanged = t;//re-seat callback
            }
        }

        public static void updateUIFloatEditControl(this PartModule module, string fieldName, float newValue)
        {
            UI_FloatEdit widget = null;
            if (HighLogic.LoadedSceneIsEditor)
            {
                widget = (UI_FloatEdit)module.Fields[fieldName].uiControlEditor;
            }
            else if (HighLogic.LoadedSceneIsFlight)
            {
                widget = (UI_FloatEdit)module.Fields[fieldName].uiControlFlight;
            }
            else
            {
                return;
            }
            if (widget == null)
            {
                return;
            }
            BaseField field = module.Fields[fieldName];
            field.SetValue(newValue, field.host);
            if (widget.partActionItem != null)//force widget re-setup for changed values; this will update the GUI value and slider positions/internal cached data
            {
                UIPartActionFloatEdit ctr = (UIPartActionFloatEdit)widget.partActionItem;
                var t = widget.onFieldChanged;//temporarily remove the callback; we don't need an event fired when -we- are the ones editing the value...            
                widget.onFieldChanged = null;
                ctr.incSmall.onToggle.RemoveAllListeners();
                ctr.incLarge.onToggle.RemoveAllListeners();
                ctr.decSmall.onToggle.RemoveAllListeners();
                ctr.decLarge.onToggle.RemoveAllListeners();
                ctr.slider.onValueChanged.RemoveAllListeners();
                ctr.Setup(ctr.Window, module.part, module, HighLogic.LoadedSceneIsEditor ? UI_Scene.Editor : UI_Scene.Flight, widget, module.Fields[fieldName]);
                widget.onFieldChanged = t;//re-seat callback
            }
        }

        public static void updateUIChooseOptionControl(this PartModule module, string fieldName, string[] options, string[] display, bool forceUpdate, string forceVal = "")
        {
            if (display.Length == 0 && options.Length > 0) { display = new string[] { "NONE" }; }
            if (options.Length == 0) { options = new string[] { "NONE" }; }
            UI_ChooseOption widget = null;
            if (HighLogic.LoadedSceneIsEditor)
            {
                widget = (UI_ChooseOption)module.Fields[fieldName].uiControlEditor;
            }
            else if (HighLogic.LoadedSceneIsFlight)
            {
                widget = (UI_ChooseOption)module.Fields[fieldName].uiControlFlight;
            }
            else { return; }
            if (widget == null) { return; }
            widget.display = display;
            widget.options = options;
            if (forceUpdate && widget.partActionItem != null)
            {
                UIPartActionChooseOption control = (UIPartActionChooseOption)widget.partActionItem;
                var t = widget.onFieldChanged;
                widget.onFieldChanged = null;
                int index = Array.IndexOf(options, forceVal);
                control.slider.minValue = 0;
                control.slider.maxValue = options.Length - 1;
                control.slider.value = index;
                control.OnValueChanged(0);
                widget.onFieldChanged = t;
            }
        }

        public static void updateUIScaleEditControl(this PartModule module, string fieldName, float[] intervals, float[] increments, bool forceUpdate, float forceValue = 0)
        {
            UI_ScaleEdit widget = null;
            if (HighLogic.LoadedSceneIsEditor)
            {
                widget = (UI_ScaleEdit)module.Fields[fieldName].uiControlEditor;
            }
            else if (HighLogic.LoadedSceneIsFlight)
            {
                widget = (UI_ScaleEdit)module.Fields[fieldName].uiControlFlight;
            }
            else
            {
                return;
            }
            if (widget == null)
            {
                return;
            }
            widget.intervals = intervals;
            widget.incrementSlide = increments;
            if (forceUpdate && widget.partActionItem != null)
            {
                UIPartActionScaleEdit ctr = (UIPartActionScaleEdit)widget.partActionItem;
                var t = widget.onFieldChanged;
                widget.onFieldChanged = null;
                ctr.inc.onToggle.RemoveAllListeners();
                ctr.dec.onToggle.RemoveAllListeners();
                ctr.slider.onValueChanged.RemoveAllListeners();
                ctr.Setup(ctr.Window, module.part, module, HighLogic.LoadedSceneIsEditor ? UI_Scene.Editor : UI_Scene.Flight, widget, module.Fields[fieldName]);
                widget.onFieldChanged = t;
            }
        }

        public static void updateUIScaleEditControl(this PartModule module, string fieldName, float min, float max, float increment, bool flight, bool editor, bool forceUpdate, float forceValue = 0)
        {
            BaseField field = module.Fields[fieldName];
            if (increment <= 0)//div/0 error
            {
                field.guiActive = false;
                field.guiActiveEditor = false;
                return;
            }
            float seg = (max - min) / increment;
            int numOfIntervals = (int)Math.Round(seg) + 1;
            float sliderInterval = increment * 0.05f;
            float[] intervals = new float[numOfIntervals];
            float[] increments = new float[numOfIntervals];
            UI_Scene scene = HighLogic.LoadedSceneIsFlight ? UI_Scene.Flight : UI_Scene.Editor;
            if (numOfIntervals <= 1)//not enough data...
            {
                field.guiActive = false;
                field.guiActiveEditor = false;
                MonoBehaviour.print("ERROR: Not enough data to create intervals: " + min + " : " + max + " :: " + increment);
            }
            else
            {
                field.guiActive = flight;
                field.guiActiveEditor = editor;
                intervals = new float[numOfIntervals];
                increments = new float[numOfIntervals];
                for (int i = 0; i < numOfIntervals; i++)
                {
                    intervals[i] = min + (increment * (float)i);
                    increments[i] = sliderInterval;
                }
                module.updateUIScaleEditControl(fieldName, intervals, increments, forceUpdate, forceValue);
            }
        }

        public static void updateUIScaleEditControl(this PartModule module, string fieldName, float value)
        {
            UI_ScaleEdit widget = null;
            if (HighLogic.LoadedSceneIsEditor)
            {
                widget = (UI_ScaleEdit)module.Fields[fieldName].uiControlEditor;
            }
            else if (HighLogic.LoadedSceneIsFlight)
            {
                widget = (UI_ScaleEdit)module.Fields[fieldName].uiControlFlight;
            }
            else
            {
                return;
            }
            if (widget == null || widget.partActionItem == null)
            {
                return;
            }
            UIPartActionScaleEdit ctr = (UIPartActionScaleEdit)widget.partActionItem;
            var t = widget.onFieldChanged;
            widget.onFieldChanged = null;
            ctr.inc.onToggle.RemoveAllListeners();
            ctr.dec.onToggle.RemoveAllListeners();
            ctr.slider.onValueChanged.RemoveAllListeners();
            ctr.Setup(ctr.Window, module.part, module, HighLogic.LoadedSceneIsEditor ? UI_Scene.Editor : UI_Scene.Flight, widget, module.Fields[fieldName]);
            widget.onFieldChanged = t;
        }

        /// <summary>
        /// Performs the input delegate onto the input part module and any modules found in symmetry counerparts.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="module"></param>
        /// <param name="action"></param>
        public static void actionWithSymmetry<T>(this T module, Action<T> action) where T : PartModule
        {
            action(module);
            forEachSymmetryCounterpart(module, action);
        }

        /// <summary>
        /// Performs the input delegate onto any modules found in symmetry counerparts. (does not effect this.module)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="module"></param>
        /// <param name="action"></param>
        public static void forEachSymmetryCounterpart<T>(this T module, Action<T> action) where T : PartModule
        {
            int index = module.part.Modules.IndexOf(module);
            int len = module.part.symmetryCounterparts.Count;
            for (int i = 0; i < len; i++)
            {
                action((T)module.part.symmetryCounterparts[i].Modules[index]);
            }
        }

        #endregion

        #region REGION - Cube and RenderTex export
        
        public static void exportCubemap(Cubemap envMap, string name)
        {
            Directory.CreateDirectory("cubeExport");
            Texture2D tex = new Texture2D(envMap.width, envMap.height, TextureFormat.ARGB32, false);
            for (int i = 0; i < 6; i++)
            {
                tex.SetPixels(envMap.GetPixels((CubemapFace)i));
                byte[] bytes = tex.EncodeToPNG();
                File.WriteAllBytes("cubeExport/" + name + "-" + i + ".png", bytes);
            }
            GameObject.Destroy(tex);
        }

        public static void exportCubemap(RenderTexture envMap, string name)
        {
            Directory.CreateDirectory("cubeExport");
            Texture2D tex = new Texture2D(envMap.width, envMap.height, TextureFormat.ARGB32, false);
            for (int i = 0; i < 6; i++)
            {
                Graphics.SetRenderTarget(envMap, 0, (CubemapFace)i);
                tex.ReadPixels(new Rect(0, 0, envMap.width, envMap.height), 0, 0);
                tex.Apply();
                byte[] bytes = tex.EncodeToPNG();
                File.WriteAllBytes("cubeExport/" + name + "-" + i + ".png", bytes);
            }
            GameObject.Destroy(tex);
        }

        public static void exportStdCube(Material mat, string name)
        {
            Texture2D tex0 = (Texture2D)mat.GetTexture("_FrontTex");
            Texture2D tex1 = (Texture2D)mat.GetTexture("_BackTex");
            Texture2D tex2 = (Texture2D)mat.GetTexture("_LeftTex");
            Texture2D tex3 = (Texture2D)mat.GetTexture("_RightTex");
            Texture2D tex4 = (Texture2D)mat.GetTexture("_UpTex");
            Texture2D tex5 = (Texture2D)mat.GetTexture("_DownTex");

            RenderTexture rt = new RenderTexture(tex0.width, tex0.height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);

            Graphics.Blit(tex0, rt);
            exportTexture(rt, name + "0");
            Graphics.Blit(tex1, rt);
            exportTexture(rt, name + "1");
            Graphics.Blit(tex2, rt);
            exportTexture(rt, name + "2");
            Graphics.Blit(tex3, rt);
            exportTexture(rt, name + "3");
            Graphics.Blit(tex4, rt);
            exportTexture(rt, name + "4");
            Graphics.Blit(tex5, rt);
            exportTexture(rt, name + "5");
        }

        public static void exportCubemapReadOnly(Cubemap envMap, string name)
        {
            RenderTexture rt = new RenderTexture(envMap.width, envMap.height, 24, RenderTextureFormat.ARGB32);
            rt.dimension = UnityEngine.Rendering.TextureDimension.Cube;
            rt.useMipMap = true;
            Graphics.Blit(envMap, rt);
            exportCubemap(rt, name);

            Texture2D tex = new Texture2D(envMap.width, envMap.height, TextureFormat.ARGB32, true);
            Graphics.CopyTexture(envMap, 0, 0, tex, 0, 0);
            exportTexture(tex, name + "-single");

            RenderTexture rt2 = new RenderTexture(tex.width, tex.height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);

            Graphics.Blit(tex, rt2);
            exportTexture(rt2, name + "-single2");
        }

        public static void exportTexture(RenderTexture envMap, string name)
        {
            Texture2D tex = new Texture2D(envMap.width, envMap.height, TextureFormat.ARGB32, false);
            Graphics.SetRenderTarget(envMap);
            tex.ReadPixels(new Rect(0, 0, envMap.width, envMap.height), 0, 0);
            tex.Apply();
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes("cubeExport/" + name + ".png", bytes);
        }

        public static void exportTexture(Texture2D tex, string name)
        {
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes("cubeExport/" + name + ".png", bytes);
        }

        public static void dumpWorldHierarchy()
        {
            GameObject[] allGos = GameObject.FindObjectsOfType<GameObject>();
            int len = allGos.Length;
            for (int i = 0; i < len; i++)
            {
                GameObject go = allGos[i];                
                if (go != null)
                {
                    if (go.transform.parent != null)//skip any non-root game-objects in this iteration
                    {
                        continue;
                    }
                    exportModelHierarchy(go);
                }
            }
        }

        public static void exportModelHierarchy(GameObject current)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine();//add carriage return to fix timestamp formatting stuff in log
            exportModelHierarchy(current, builder, 0);
            MonoBehaviour.print(builder.ToString());
        }

        private static void exportModelHierarchy(GameObject current, StringBuilder builder, int indent)
        {
            builder.Append(' ', indent);
            builder.AppendLine("Game Object: " + current.name);
            builder.Append(' ', indent + 3);
            builder.AppendLine("parent  : " + current.transform.parent);
            builder.Append(' ', indent+3);
            builder.AppendLine("position: " + current.transform.position);
            builder.Append(' ', indent+3);
            builder.AppendLine("scale   : " + current.transform.localScale);
            builder.Append(' ', indent+3);
            builder.AppendLine("rotation: " + current.transform.rotation);
            builder.Append(' ', indent+3);
            builder.AppendLine("layer   : " + current.layer);
            builder.Append(' ', indent+3);
            builder.AppendLine("active  : " + current.activeSelf);
            Renderer rend = current.GetComponent<Renderer>();
            Material mat;
            if (rend != null && (mat = rend.material) != null)
            {
                builder.AppendLine(Debug.getMaterialPropertiesDebug(mat, indent));
            }
            foreach (Transform tr in current.transform)
            {
                exportModelHierarchy(tr.gameObject, builder, indent + 3);
            }
        }

        public static void dumpCameraData()
        {
            Camera[] cams = GameObject.FindObjectsOfType<Camera>();
            int len = cams.Length;
            for (int i = 0; i < len; i++)
            {
                Camera cam = cams[i];
                string output = "camera: " + cam.name + "," + cam.gameObject + "," + cam.gameObject.transform.parent + "," + cam.cullingMask + "," + cam.nearClipPlane + "," + cam.farClipPlane + "," + cam.transform.position.x + "," + cam.transform.position.y + "," + cam.transform.position.z;
                MonoBehaviour.print(output);
            }
        }

        public static void dumpReflectionData()
        {
            MonoBehaviour.print("------------------REFLECTION DATA--------------------");
            MonoBehaviour.print("Reflection probes found in scene:");
            ReflectionProbe[] probes = GameObject.FindObjectsOfType<ReflectionProbe>();
            int len = probes.Length;
            for (int i = 0; i < len; i++)
            {
                MonoBehaviour.print(string.Format("ReflectionProbe[{0}] : Object Parent: {1}  Probe: {2}", i, probes[i].gameObject, probes[i]));
            }
            Material mat = RenderSettings.skybox;
            MonoBehaviour.print("Rendersetting skybox: " + mat);
            if (mat != null)
            {
                Texture tex = mat.GetTexture("_Tex");
                MonoBehaviour.print("skybox shader: " + mat.shader);
                MonoBehaviour.print("skybox texture: " + tex);
                Cubemap cube = tex as Cubemap;
                if (cube != null)
                {
                    Utils.exportCubemap(cube, "RenderSettings-Skybox");
                }
                RenderTexture rTex = tex as RenderTexture;
                if (rTex != null)
                {
                    Utils.exportCubemap(rTex, "RenderSettings-Skybox");
                }
                if (tex == null)//not a std cubemap shader, check for six-sided
                {
                    exportStdCube(mat, "RenderSettings-Skybox");
                }
            }
            Skybox[] skies = GameObject.FindObjectsOfType<Skybox>();
            len = skies.Length;
            for (int i = 0; i < len; i++)
            {
                Skybox sky = skies[i];
                mat = sky.material;
                MonoBehaviour.print(string.Format("Camera Skybox[{0}]: {1}  Material: {2} ", i, sky, mat));
            }
            Cubemap cube2 = RenderSettings.customReflection;
            if (cube2 != null)
            {
                MonoBehaviour.print("Custom cube reflection: " + cube2);
                exportCubemapReadOnly(cube2, "RenderSettings-CustomReflection");
            }
            MonoBehaviour.print("------------------REFLECTION DATA--------------------");
        }

        #endregion
    }
}
