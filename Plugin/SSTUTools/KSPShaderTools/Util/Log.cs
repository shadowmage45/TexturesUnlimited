using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSPShaderTools
{

    public static class Log
    {

        public static void log(object message)
        {
            MonoBehaviour.print(message);
        }

        public static void exception(object message)
        {
            MonoBehaviour.print(message);
        }

        public static void debug(object message)
        {
            MonoBehaviour.print(message);
        }

        public static void error(object message)
        {
            if (TexturesUnlimitedLoader.logErrors)
            {
                MonoBehaviour.print(message);
            }
        }

        public static void replacement(object message)
        {
            if (TexturesUnlimitedLoader.logReplacements)
            {
                MonoBehaviour.print(message);
            }
        }

        public static void extra(object message)
        {
            if (TexturesUnlimitedLoader.logAll)
            {
                MonoBehaviour.print(message);
            }
        }

    }

}
