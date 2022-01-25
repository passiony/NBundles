    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;

    public class SymbolsSetting
    {

        private static string LOGGERON_SYMBOLS = "LOGGER_ON";
        private static string ENCRYPT_SYMBOLS = "ENCRYPT";
        private static string DEVELOP_SYMBOLS = "DEVELOPMENT";
        private static string PURCHASING_SYMBOLS = "UNITY_PURCHASING";
        private static string VALIDATION_SYMBOLS = "RECEIPT_VALIDATION";
        private static string MopubManager_SYMBOLS = "mopub_manager";
        private static string CN_IP_SYMBOLS = "CN_IP";
        private static string USA_IP_SYMBOLS = "USA_IP";
        private static string DEBUG_IP1_SYMBOLS = "DEBUG_IP1";
        private static string DEBUG_IP2_SYMBOLS = "DEBUG_IP2";
        
        public static bool isLoggerOn { get; private set; }
        public static bool isEncrypt { get; private set; }
        public static bool isDevelop { get; private set; }
        public static bool isPurchasing { get; private set; }
        public static bool isValidation { get; private set; }
        public static ServerType serverType { get; private set; }
        
        public static void Init()
        {
            BuildTargetGroup targetGroup = GetActiveTargetGroup();
            string symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);

            isLoggerOn = symbols.Contains(LOGGERON_SYMBOLS);
            isEncrypt = symbols.Contains(ENCRYPT_SYMBOLS);
            isDevelop = symbols.Contains(DEVELOP_SYMBOLS);
            isPurchasing = symbols.Contains(PURCHASING_SYMBOLS);
            isValidation = symbols.Contains(VALIDATION_SYMBOLS);

            if (symbols.Contains(CN_IP_SYMBOLS))
                serverType = ServerType.CN_IP;
            else if (symbols.Contains(USA_IP_SYMBOLS))
                serverType = ServerType.USA_IP;
        }
        
        public static void SetLoggerOn(bool enable)
        {
            isLoggerOn = enable;
            CompileSymbols();
        }

        public static void SetEncrypt(bool enable)
        {
            isEncrypt = enable;
            CompileSymbols();
        }
        
        public static void SetDevelop(bool enable)
        {
            isDevelop = enable;
            CompileSymbols();
        }

        public static void SetPurchasing(bool enable)
        {
            isPurchasing = enable;
            CompileSymbols();
        }
        
        public static void SetValidation(bool enable)
        {
            isValidation = enable;
            CompileSymbols();
        }

        public static void SetServerType(ServerType type)
        {
            serverType = type;
            CompileSymbols();
        }
        
        public static void CompileSymbols()
        {
            HashSet<string> defineSymbols = new HashSet<string>();
            defineSymbols.Add(MopubManager_SYMBOLS);
            if (isLoggerOn)
            {
                defineSymbols.Add(LOGGERON_SYMBOLS);
            }
            if (isEncrypt)
            {
                defineSymbols.Add(ENCRYPT_SYMBOLS);
            }
            if (isDevelop)
            {
                defineSymbols.Add(DEVELOP_SYMBOLS);
            }
            if (isPurchasing)
            {
                defineSymbols.Add(PURCHASING_SYMBOLS);
            }
            if (isValidation)
            {
                defineSymbols.Add(VALIDATION_SYMBOLS);
            }
            
            if (serverType == ServerType.CN_IP)
                defineSymbols.Add(CN_IP_SYMBOLS);
            else if (serverType == ServerType.USA_IP)
                defineSymbols.Add(USA_IP_SYMBOLS);
            
            string result = string.Join(";", defineSymbols.ToArray());
            PlayerSettings.SetScriptingDefineSymbolsForGroup(GetActiveTargetGroup(), result);
            Debug.LogWarning("Apply Symbols->" + result);
            AssetDatabase.Refresh();
        }

        
        public static BuildTargetGroup GetActiveTargetGroup()
        {
            switch (EditorUserBuildSettings.activeBuildTarget)
            {
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    return BuildTargetGroup.Standalone;
                case BuildTarget.Android:
                    return BuildTargetGroup.Android;
                case BuildTarget.iOS:
                    return BuildTargetGroup.iOS;
                default:
                    return BuildTargetGroup.Android;
            }
        }
    }
    