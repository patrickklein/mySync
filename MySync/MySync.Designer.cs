﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.18444
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace My_Sync {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "11.0.0.0")]
    internal sealed partial class MySync : global::System.Configuration.ApplicationSettingsBase {
        
        private static MySync defaultInstance = ((MySync)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new MySync())));
        
        public static MySync Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("D:\\Studium\\MSC - Softwareentwicklung\\3. Semester\\Master Projekt\\Projekt\\Code\\Log")]
        public string logPath {
            get {
                return ((string)(this["logPath"]));
            }
            set {
                this["logPath"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool logState {
            get {
                return ((bool)(this["logState"]));
            }
            set {
                this["logState"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("de-AT")]
        public string usedLanguage {
            get {
                return ((string)(this["usedLanguage"]));
            }
            set {
                this["usedLanguage"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("MySync")]
        public string mainFolder {
            get {
                return ((string)(this["mainFolder"]));
            }
            set {
                this["mainFolder"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool runAtStartup {
            get {
                return ((bool)(this["runAtStartup"]));
            }
            set {
                this["runAtStartup"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool showNotification {
            get {
                return ((bool)(this["showNotification"]));
            }
            set {
                this["showNotification"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool addToFavorites {
            get {
                return ((bool)(this["addToFavorites"]));
            }
            set {
                this["addToFavorites"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool fastSync {
            get {
                return ((bool)(this["fastSync"]));
            }
            set {
                this["fastSync"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("90")]
        public string synchronizationInterval {
            get {
                return ((string)(this["synchronizationInterval"]));
            }
            set {
                this["synchronizationInterval"] = value;
            }
        }
    }
}
