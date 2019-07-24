using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSPShaderTools.Addon
{

    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class EditorReflectionUpdate : MonoBehaviour
    {

        private bool fixedRP;
        private GameObject probeObject;
        private ReflectionProbe probe;

        public void Awake()
        {
            fixedRP = false;
            Log.debug("EditorReflectionUpdate - Awake()");
            ReflectionProbe v = (ReflectionProbe)GameObject.FindObjectOfType(typeof(ReflectionProbe));
            Log.debug("Probe: " + v);
        }

        public void Start()
        {
            Log.debug("EditorReflectionUpdate - Start()");
            ReflectionProbe v = (ReflectionProbe)GameObject.FindObjectOfType(typeof(ReflectionProbe));
            Log.debug("Probe: " + v);
            probeObject = new GameObject("TUEditorReflectionProbe");
            probeObject.transform.position = new Vector3(0, 10, 0);
            probe = probeObject.AddComponent<ReflectionProbe>();
            probe.size = new Vector3(1000,1000,1000);
            probe.resolution = 512;
            probe.hdr = false;
            probe.cullingMask = (1 << 4) | (1 << 15) | (1 << 17) | (1 << 23) | (1 << 10) | (1 << 9) | (1 << 18);//everything the old reflection probe system captured
            probe.enabled = true;
            probe.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.ViaScripting;
            probe.timeSlicingMode = UnityEngine.Rendering.ReflectionProbeTimeSlicingMode.NoTimeSlicing;
            probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
            probe.nearClipPlane = 0.1f;
            probe.farClipPlane = 2000f;
            probe.boxProjection = false;
            probe.clearFlags = UnityEngine.Rendering.ReflectionProbeClearFlags.SolidColor;
            probe.backgroundColor = Color.black;
        }

        private int delay = 5;
        public void Update()
        {
            if (!fixedRP)
            {
                if (delay > 0)
                {
                    delay--;
                    return;
                }
                fixedRP = true;
                Log.debug("EditorReflectionUpdate - Update()");
                Log.debug("Probe: " + probe + " : " + probe.gameObject.name);
                probe.RenderProbe();
            }
        }

    }

}
