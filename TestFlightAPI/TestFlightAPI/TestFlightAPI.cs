using System;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;
using UnityEngine.Profiling;

namespace TestFlightAPI
{
    public enum InteropValueType
    {
        INVALID = -1,
        STRING,
        FLOAT,
        INT,
        BOOL,
        STRING_LIST,
        FLOAT_LIST,
        INT_LIST,
        BOOL_LIST
    }

    public enum RatingScope
    {
        Cumulative,
        Continuous
    }

    public struct InteropValue
    {
        public InteropValueType valueType;
        public string value;
        public string owner;
    };

    public static class TestFlightUtil
    {
        public const double MIN_FAILURE_RATE = 0.00000000000001f;

        private static Type[] testFlightModulesTypes = {typeof(IFlightDataRecorder), typeof(ITestFlightFailure), typeof(ITestFlightReliability)};

        public enum MTBFUnits : int
        {
            SECONDS = 0,
            MINUTES,
            HOURS,
            DAYS,
            YEARS,
            INVALID}

        ;

        public enum TIMEFORMAT : int
        {
            COLON_SEPERATED = 0,
            SHORT_IDENTIFIER,
            LONG_IDENTIFIER}

        ;

        public static int HoursPerDay { get { return GameSettings.KERBIN_TIME ? 6 : 24; } }

        public static int DaysPerYear { get { return GameSettings.KERBIN_TIME ? 426 : 365; } }

        public static Type[] TestFlightModulesTypes
        {
            get
            {
                return testFlightModulesTypes;
            }

            set
            {
                testFlightModulesTypes = value;
            }
        }

        public static string FormatTime(float time, TIMEFORMAT format, bool richText)
        {
            if (double.IsInfinity(time) || double.IsNaN(time))
                return "Inf";

            string ret = "";

            try
            {
                List<string> units = new List<string>();
                switch (format)
                {
                    case TIMEFORMAT.LONG_IDENTIFIER:
                        if (richText)
                            units.AddRange(new string[] {" <b>years</b>, ", " <b>days</b>, ", " <b>hours</b>, ", " <b>minutes</b>, ", " <b>seconds</b>" } );
                        else
                            units.AddRange(new string[] {" years, ", " days, ", " hours, ", " minutes, ", " seconds" } );
                        break;
                    case TIMEFORMAT.SHORT_IDENTIFIER:
                        if (richText)
                            units.AddRange(new string[] {"<b>y</b> ", "<b>d</b> ", "<b>h</b> ", "<b>m</b> ", "<b>s</b>" } );
                        else
                            units.AddRange(new string[] {"y ", "d ", "h ", "m ", "s" } );
                        break;
                    default:
                        units.AddRange(new string[] {":", ":", ":", ":", ":" } );
                        break;
                }
                List<float> intervals = new List<float>();
                intervals.AddRange(new float[] {DaysPerYear * HoursPerDay * 3600, HoursPerDay * 3600, 3600, 60, 1});

                for (int i = 0; i < units.Count; i++)
                {
                    int amount = Mathf.FloorToInt(time / intervals[i]);
                    if (amount > 0)
                    {
                        ret += string.Format("{0}{1}", amount, units[i]);
                        time -= amount * intervals[i];
                    }
                }
            }
            catch (Exception)
            {
                return "NaN";
            }
            return ret;
        }

        public static string FailureRateToMTBFString(double failureRate, MTBFUnits units, bool shortForm, int maximum)
        {
            var currentUnit = (int)MTBFUnits.SECONDS;
            var mtbf = FailureRateToMTBF(failureRate, (MTBFUnits)currentUnit);
            while (mtbf > maximum)
            {
                currentUnit++;
                mtbf = FailureRateToMTBF(failureRate, (MTBFUnits)currentUnit);
                if ((MTBFUnits)currentUnit == MTBFUnits.INVALID)
                    break;
            }

            if (shortForm)
            {
                return string.Format("{0:F2}{1}", mtbf, UnitStringForMTBFUnit((MTBFUnits)currentUnit)[0]);
            }
            else
            {
                return string.Format("{0:F2} {1}", mtbf, UnitStringForMTBFUnit((MTBFUnits)currentUnit));
            }
        }

        public static void FailureRateToMTBFString(double failureRate, MTBFUnits units, bool shortForm,
            int maximum, out string output)
        {
            var currentUnit = (int)MTBFUnits.SECONDS;
            var mtbf = FailureRateToMTBF(failureRate, (MTBFUnits)currentUnit);
            while (mtbf > maximum)
            {
                currentUnit++;
                mtbf = FailureRateToMTBF(failureRate, (MTBFUnits)currentUnit);
                if ((MTBFUnits)currentUnit == MTBFUnits.INVALID)
                    break;
            }

            if (shortForm)
            {
                output = string.Format("{0:F2}{1}", mtbf,
                    UnitStringForMTBFUnitShort((MTBFUnits)currentUnit)).ToString();
            }
            else
            {
                output = string.Format("{0:F2}{1}", mtbf,
                    UnitStringForMTBFUnit((MTBFUnits)currentUnit)).ToString();
            }
        }

        // Simply converts the failure rate to a MTBF number, without any string formatting
        public static double FailureRateToMTBF(double failureRate, MTBFUnits units)
        {
            failureRate = Math.Max(failureRate, MIN_FAILURE_RATE);
            double mtbfSeconds = 1.0f / failureRate;

            switch (units)
            {
                case MTBFUnits.SECONDS:
                    return mtbfSeconds;
                case MTBFUnits.MINUTES:
                    return mtbfSeconds / 60;
                case MTBFUnits.HOURS:
                    return mtbfSeconds / 60 / 60;
                case MTBFUnits.DAYS:
                    return mtbfSeconds / 60 / 60 / 24;
                case MTBFUnits.YEARS:
                    return mtbfSeconds / 60 / 60 / 24 / 365;
                default:
                    return mtbfSeconds;
            }
        }

        internal static string UnitStringForMTBFUnit(MTBFUnits units)
        {
            switch (units)
            {
                case MTBFUnits.SECONDS:
                    return "seconds";
                case MTBFUnits.MINUTES:
                    return "minutes";
                case MTBFUnits.HOURS:
                    return "hours";
                case MTBFUnits.DAYS:
                    return "days";
                case MTBFUnits.YEARS:
                    return "years";
                default:
                    return "invalid";
            }
        }
        internal static string UnitStringForMTBFUnitShort(MTBFUnits units)
        {
            switch (units)
            {
                case MTBFUnits.SECONDS:
                    return "s";
                case MTBFUnits.MINUTES:
                    return "m";
                case MTBFUnits.HOURS:
                    return "h";
                case MTBFUnits.DAYS:
                    return "d";
                case MTBFUnits.YEARS:
                    return "y";
                default:
                    return "-";
            }
        }
        
        // Simply converts the failure rate to the reliability rate at time t.
        public static float FailureRateToReliability(float failureRate, float t)
        {
            float reliability = Mathf.Exp(-failureRate * t);
            return reliability;
        }
        public static double FailureRateToReliability(double failureRate, float t)
        {
            double reliability = Math.Exp(-failureRate * t);
            return reliability;
        }

        public static string FormatTime(double time)
        {
            return FormatTime((float)time, TIMEFORMAT.COLON_SEPERATED, false);
        }

        // Methods for accessing the TestFlight modules on a given part
        // Get the part name, minus any numbers or clone or whatever. Borrowed from RF (by NK, so by permission obviously).
        public static string GetPartName(Part part)
        {
            if (part.partInfo != null)
                return GetPartName(part.partInfo);
            return GetPartName(part.name);
        }

        public static string GetPartName(AvailablePart ap)
        {
            return GetPartName(ap.name);
        }

        public static string GetPartName(string partName)
        {
            partName = partName.Replace(".", "-");
            return partName.Replace("_", "-");
        }
            
        public static ITestFlightCore ResolveAlias(Part part, string alias)
        {
            // Look at each Core on the part and find the one that defines the given alias
            // If not found return null
            // If found, evaluate its query and return the core if true, return null if false

            return null;
        }

        public static string GetPartTitle(Part part, string alias)
        {
            string baseName = part.partInfo.title;

            if (part.Modules == null)
                return baseName;

            // Find the active core
            ITestFlightCore core = TestFlightUtil.GetCore(part, alias);
            if (core == null)
                return baseName;

            if (String.IsNullOrEmpty(core.Title))
                return baseName;
            else
                return core.Title;
        }
        // Get the active Core Module - can only ever be one.
        public static ITestFlightCore GetCore(Part part)
        {
            Profiler.BeginSample("GetCore1");
            if (part == null || part.Modules == null)
                return null;

            for (int i = 0; i < part.Modules.Count; i++)
            {
                ITestFlightCore core = part.Modules[i] as ITestFlightCore;
                if (core != null && core.TestFlightEnabled)
                    return core;
            }
            Profiler.EndSample();
            return null;
        }

        public static ITestFlightCore GetCore(Part part, string alias)
        {
            if (part == null || part.Modules == null)
                return null;
            if (alias == "")
                return null;
            for (int i = 0, partModulesCount = part.Modules.Count; i < partModulesCount; i++)
            {
                PartModule pm = part.Modules[i];
                ITestFlightCore core = pm as ITestFlightCore;
                if (core != null)
                {
                    core.UpdatePartConfig();
                    return core;
                }
            }
            return null;
        }

        public static void UpdatePartConfigs(Part part)
        {
            for (int i = 0, partModulesCount = part.Modules.Count; i < partModulesCount; i++)
            {
                PartModule pm = part.Modules[i];
                ITestFlightCore core = pm as ITestFlightCore;
                if (core != null)
                {
                    core.UpdatePartConfig();
                }
            }
        }
        
        public static List<PartModule> GetAllTestFlightModulesForPart(Part part)
        {
            if (part == null || part.Modules == null)
                return null;

            List<PartModule> modules = new List<PartModule>();
            for (int i = 0, partModulesCount = part.Modules.Count; i < partModulesCount; i++)
            {
                PartModule pm = part.Modules[i];
                
                // FlightDataRecorder
                IFlightDataRecorder dataRecorder = pm as IFlightDataRecorder;
                if (dataRecorder != null)
                {
                    modules.Add(pm);
                    continue;
                }
                // TestFlightReliability
                ITestFlightReliability reliabilityModule = pm as ITestFlightReliability;
                if (reliabilityModule != null)
                {
                    modules.Add(pm);
                    continue;
                }
                // TestFlightFailure
                ITestFlightFailure failureModule = pm as ITestFlightFailure;
                if (failureModule != null)
                {
                    modules.Add(pm);
                    continue;
                }
            }

            return modules;
        }
        

        public static List<PartModule> GetAllTestFlightModulesForAlias(Part part, string alias)
        {
            if (part == null || part.Modules == null)
                return null;

            List<PartModule> modules = new List<PartModule>();
            for (int i = 0, partModulesCount = part.Modules.Count; i < partModulesCount; i++)
            {
                PartModule pm = part.Modules[i];
                
                // FlightDataRecorder
                IFlightDataRecorder dataRecorder = pm as IFlightDataRecorder;
                if (dataRecorder != null && dataRecorder.Configuration.Equals(alias, StringComparison.InvariantCultureIgnoreCase))
                {
                    modules.Add(pm);
                    continue;
                }
                // TestFlightReliability
                ITestFlightReliability reliabilityModule = pm as ITestFlightReliability;
                if (reliabilityModule != null &&
                    reliabilityModule.Configuration.Equals(alias, StringComparison.InvariantCultureIgnoreCase))
                {
                    modules.Add(pm);
                    continue;
                }
                // TestFlightFailure
                ITestFlightFailure failureModule = pm as ITestFlightFailure;
                if (failureModule != null &&
                    failureModule.Configuration.Equals(alias, StringComparison.InvariantCultureIgnoreCase))
                {
                    modules.Add(pm);
                    continue;
                }
            }

            return modules;
        }

        // Get the Data Recorder Module - can only ever be one.
        public static IFlightDataRecorder GetDataRecorder(Part part, string alias)
        {
            if (part == null)
                return null;

            return part.GetComponent(typeof(IFlightDataRecorder)) as IFlightDataRecorder;
        }
        // Get all Reliability Modules - can be more than one.
        public static List<ITestFlightReliability> GetReliabilityModules(Part part, string alias, bool checkEnabled = true)
        {
            if (part == null || part.Modules == null)
                return null;

            var reliabilityModules = new List<ITestFlightReliability>();
            for (int i = 0, partModulesCount = part.Modules.Count; i < partModulesCount; i++)
            {
                PartModule pm = part.Modules[i];
                ITestFlightReliability reliabilityModule = pm as ITestFlightReliability;
                if (reliabilityModule != null)
                {
                    // reliabilityModule.SetActiveConfig(alias);
                    reliabilityModules.Add(reliabilityModule);
                }
            }

            return reliabilityModules;
        }
        // Get all Failure Modules - can be more than one.
        public static List<ITestFlightFailure> GetFailureModules(Part part, string alias, bool checkEnabled = true)
        {
            if (part == null || part.Modules == null)
                return null;

            var failureModules = new List<ITestFlightFailure>();
            for (int i = 0, partModulesCount = part.Modules.Count; i < partModulesCount; i++)
            {
                PartModule pm = part.Modules[i];
                ITestFlightFailure failureModule = pm as ITestFlightFailure;
                if (failureModule != null)
                {
                    // failureModule.SetActiveConfig(alias);
                    failureModules.Add(failureModule);
                }
            }

            return failureModules;
        }

        public static ITestFlightInterop GetInteropModule(Part part)
        {
            if (part == null || part.Modules == null)
                return null;

            if (part.Modules.Contains("TestFlightInterop"))
                return part.Modules["TestFlightInterop"] as ITestFlightInterop;

            return null;
        }

        // Originally `configuration` was just a string to match again ModuleEngineConfigs property of the same name.
        // But with the expansion to work with Procedural Parts, it is getting more complicated.  Thus it is probably
        // best to split it out into a common method for all handling of configuration matches
        public static bool EvaluateQuery(string query, Part part)
        {
            Profiler.BeginSample("EvaluateQuery");
            if (string.IsNullOrEmpty(query))
                return true;

            // If this query defines an alias, just trim it off
            if (query.Contains(":"))
            {
                query = query.Split(new char[1]{ ':' })[0];
            }

            // split into list elements.  For a query to be valid only one list element has to evaluate to true
            string[] elements = query.Split(new char[1] { ',' });
            for (int i = 0, elementsCount = elements.Length; i < elementsCount; i++)
            {
                if (EvaluateElement(elements[i], part))
                    return true;
            }

            Profiler.EndSample();
            return false;
        }

        public static bool EvaluateElement(string element, Part part)
        {
            // If the element contains conditionals, then it needs to be further broken down and those conditions evaluated left to right
            // otherwise we just evaluate the block
            if (element.Contains("||"))
            {
                string[] orSections = element.Split(new[] { "||" }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0, orSectionsCount = orSections.Length; i < orSectionsCount; i++)
                {
                    if (orSections[i].Trim().Contains("&&"))
                    {
                        bool sectionIsTrue = true;
                        string[] andSections = orSections[i].Trim().Split(new string[1] { "&&" }, StringSplitOptions.RemoveEmptyEntries);
                        for (int j = 0, andSectionsCount = andSections.Length; j < andSectionsCount; j++)
                        {
                            if (!EvaluateBlock(andSections[j].Trim(), part))
                            {
                                sectionIsTrue = false;
                                break;
                            }
                        }
                        if (sectionIsTrue)
                            return true;
                    }
                    else
                    {
                        if (EvaluateBlock(orSections[i], part))
                            return true;
                    }
                }
            }
            else
                return EvaluateBlock(element, part);
            return false;
        }

        public static bool EvaluateBlock(string block, Part part)
        {
            block = block.ToLower();
            // The meat of the evaluation is done here
            if (block.Contains(" "))
            {
                string[] parts = block.Split(new char[1] { ' ' });
                if (parts.Length < 3)
                    return false;

                string qualifier = parts[0];
                string op = parts[1];
                string term = parts[2];
                string term1 = "";
                string term2 = "";

                if (term.Contains("-"))
                {
                    term1 = term.Split(new char[1]{ '-' })[0];
                    term2 = term.Split(new char[1]{ '-' })[1];
                }
                // try to get the interop value for this operator
                ITestFlightInterop interop = TestFlightUtil.GetInteropModule(part);
                if (interop == null)
                    return false;
                var val = interop.GetInterop(qualifier);
                if (val.valueType == InteropValueType.INVALID)
                    return false;
                switch (op)
                {
                    case "=":
                        switch (val.valueType)
                        {
                            case InteropValueType.BOOL:
                                return bool.Parse(val.value) == bool.Parse(term);
                            case InteropValueType.FLOAT:
                                return Math.Abs(float.Parse(val.value) - float.Parse(term)) < .0001;
                            case InteropValueType.INT:
                                return int.Parse(val.value) == int.Parse(term);
                            case InteropValueType.STRING:
                                return string.Equals(val.value, term, StringComparison.InvariantCultureIgnoreCase);
                        }
                        break;
                    case "!=":
                        switch (val.valueType)
                        {
                            case InteropValueType.BOOL:
                                return bool.Parse(val.value) != bool.Parse(term);
                            case InteropValueType.FLOAT:
                                return Math.Abs(float.Parse(val.value) - float.Parse(term)) > .0001;
                            case InteropValueType.INT:
                                return int.Parse(val.value) != int.Parse(term);
                            case InteropValueType.STRING:
                                return !string.Equals(val.value, term, StringComparison.InvariantCultureIgnoreCase);
                        }
                        break;
                    case "<":
                        switch (val.valueType)
                        {
                            case InteropValueType.FLOAT:
                                return float.Parse(val.value) < float.Parse(term);
                            case InteropValueType.INT:
                                return int.Parse(val.value) < int.Parse(term);
                        }
                        break;
                    case ">":
                        switch (val.valueType)
                        {
                            case InteropValueType.FLOAT:
                                return float.Parse(val.value) > float.Parse(term);
                            case InteropValueType.INT:
                                return int.Parse(val.value) > int.Parse(term);
                        }
                        break;
                    case "<=":
                        switch (val.valueType)
                        {
                            case InteropValueType.FLOAT:
                                return float.Parse(val.value) <= float.Parse(term);
                            case InteropValueType.INT:
                                return int.Parse(val.value) <= int.Parse(term);
                        }
                        break;
                    case ">=":
                        switch (val.valueType)
                        {
                            case InteropValueType.FLOAT:
                                return float.Parse(val.value) >= float.Parse(term);
                            case InteropValueType.INT:
                                return int.Parse(val.value) >= int.Parse(term);
                        }
                        break;
                    case "<>":
                        switch (val.valueType)
                        {
                            case InteropValueType.FLOAT:
                                return float.Parse(val.value) > float.Parse(term1) && float.Parse(val.value) < float.Parse(term2);
                            case InteropValueType.INT:
                                return int.Parse(val.value) > int.Parse(term1) && int.Parse(val.value) < int.Parse(term1);
                        }
                        break;
                    case "<=>":
                        switch (val.valueType)
                        {
                            case InteropValueType.FLOAT:
                                return float.Parse(val.value) >= float.Parse(term1) && float.Parse(val.value) <= float.Parse(term2);
                            case InteropValueType.INT:
                                return int.Parse(val.value) >= int.Parse(term1) && int.Parse(val.value) <= int.Parse(term1);
                        }
                        break;
                }
                return false;
            }
            else
            {
                return false;
            }
        }

        public static string Configuration(Part part)
        {
            if (!part.Modules.Contains("ModuleEngineConfigs")) return "";
            string configuration = (string)(part.Modules["ModuleEngineConfigs"].GetType().GetField("configuration").GetValue(part.Modules["ModuleEngineConfigs"]));
            return configuration.ToLower();
        }

        public static void Log(string message, Part loggingPart)
        {
            return;
            //            ITestFlightCore core = TestFlightUtil.GetCore(loggingPart);
            //            bool debug = false;
            //            if (core != null)
            //                debug = core.DebugEnabled;
            //            TestFlightUtil.Log(message, debug);
        }

        public static void Log(string message, bool debug)
        {
            if (debug)
                Debug.Log("[TestFlight] " + message);
        }

        // This block of methods allow for interrogating the scenario's data store in various ways
        public static string PartWithMostData()
        {
            Type tfInterface = null;
            bool connected = false;

            try
            {
                tfInterface = Type.GetType("TestFlightCore.TestFlightInterface, TestFlightCore");
            }
            catch
            {
                return "";
            }
            connected = (bool)tfInterface.InvokeMember("TestFlightInstalled", BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static, null, null, null);
            if (connected)
            {
                connected = (bool)tfInterface.InvokeMember("TestFlightReady", BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static, null, null, null);
            }

            if (!connected)
                return "";
            else
                return (string)tfInterface.InvokeMember("PartWithMostData", BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static, null, null, null);
        }

        public static string PartWithLeastData()
        {
            Type tfInterface = null;
            bool connected = false;

            try
            {
                tfInterface = Type.GetType("TestFlightCore.TestFlightInterface, TestFlightCore");
            }
            catch
            {
                return "";
            }
            connected = (bool)tfInterface.InvokeMember("TestFlightInstalled", BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static, null, null, null);
            if (connected)
            {
                connected = (bool)tfInterface.InvokeMember("TestFlightReady", BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static, null, null, null);
            }

            if (!connected)
                return "";
            else
                return (string)tfInterface.InvokeMember("PartWithLeastData", BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static, null, null, null);
        }

        public static string PartWithNoData(string partList)
        {
            Type tfInterface = null;
            bool connected = false;

            try
            {
                tfInterface = Type.GetType("TestFlightCore.TestFlightInterface, TestFlightCore");
            }
            catch
            {
                return "";
            }
            connected = (bool)tfInterface.InvokeMember("TestFlightInstalled", BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static, null, null, null);
            if (connected)
            {
                connected = (bool)tfInterface.InvokeMember("TestFlightReady", BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static, null, null, null);
            }

            if (!connected)
                return "";
            else
                return (string)tfInterface.InvokeMember("PartWithNoData", BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static, null, null, new object[] { partList });
        }

        public static TestFlightPartData GetPartDataForPart(string partName)
        {
            Type tfInterface = null;
            bool connected = false;

            try
            {
                tfInterface = Type.GetType("TestFlightCore.TestFlightInterface, TestFlightCore");
            }
            catch
            {
                return null;
            }
            connected = (bool)tfInterface.InvokeMember("TestFlightInstalled", BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static, null, null, null);
            if (connected)
            {
                connected = (bool)tfInterface.InvokeMember("TestFlightReady", BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static, null, null, null);
            }

            if (!connected)
                return null;
            else
                return (TestFlightPartData)tfInterface.InvokeMember("GetPartDataForPart", BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static, null, null, new object[] { partName });
        }

        public static float Integrate(Func<float, float> f, float begin, float end, float stepSize = 0.5f)
        {
            float left = begin;
            float prevValue = f(left);
            float acc = 0f;
            while (left < end)
            {
                left += stepSize;
                float value = f(left);
                acc += (prevValue + value) * stepSize * 0.5f;
                prevValue = value;
            }
            return acc;
        }
    }

    public struct TestFlightFailureDetails
    {
        // Human friendly title to display in the MSD for the failure.  25 characters max
        public string failureTitle;
        // "minor", or "major" used to indicate the severity of the failure to the player
        public string severity;
        // chances of the failure occuring relative to other failure modules on the same part
        // This should never be anything except:
        // 2 = Rare, 4 = Seldom, 8 = Average, 16 = Often, 32 = Common
        public int weight;
        // "mechanical" indicates a physical failure that requires physical repair
        // "software" indicates a software or electric failure that might be fixed remotely by code
        public string failureType;
    }

    public struct RepairRequirements
    {
        // Player friendly string explaining the requirement.  Should be kept short as is feasible
        public string requirementMessage;
        // Is the requirement currently met?
        public bool requirementMet;
        // Is this an optional requirement that will give a repair bonus if met?
        public bool optionalRequirement;
        // Repair chance bonus (IE 0.05 = +5%) if the optional requirement is met
        public float repairBonus;
    }

    public struct MomentaryFailureModifier
    {
        public String owner;
        public String triggerName;
        public double modifier;
        // ALWAYS check if valid == true before using the data in this structure!
        // If valid is false, then the data is empty because a valid data set could not be located
        public bool valid;
    }

    public struct MomentaryFailureRate
    {
        public String triggerName;
        public double failureRate;
        // ALWAYS check if valid == true before using the data in this structure!
        // If valid is false, then the data is empty because a valid data set could not be located
        public bool valid;
    }

    public interface IFlightDataRecorder
    {
        bool TestFlightEnabled
        {
            get;
        }

        string Configuration
        {
            get;
            set;
        }

        /// <summary>
        /// Returns whether or not the part is considered to be operating or running.  IE is an engine actually turned on and thrusting?  Is a command pod supplied with electricity and operating?
        /// The point of this is to distinguish between the life time of a part and the operating time of a part, which might be smaller than its total lifetime.
        /// </summary>
        /// <returns><c>true</c> if this instance is part operating; otherwise, <c>false</c>.</returns>
        bool IsPartOperating();

        /// <summary>
        /// Should return a string if the module wants to report any information to the user in the TestFlight Editor window.
        /// </summary>
        /// <returns>A string of information to display to the user, or "" if none</returns>
        List<string> GetTestFlightInfo();

        void SetActiveConfig(string configuration);
    }

    public interface ITestFlightReliability
    {
        bool TestFlightEnabled
        {
            get;
        }

        string Configuration
        {
            get;
            set;
        }
        // New API
        // Get the base or static failure rate for the given scope
        // !! IMPORTANT: Only ONE Reliability module may return a Base Failure Rate.  Additional modules can exist only to supply Momentary rates
        // If this Reliability module's purpose is to supply Momentary Fialure Rates, then it MUST return 0 when asked for the Base Failure Rate
        // If it dosn't, then the Base Failure Rate of the part will not be correct.
        /// <summary>
        /// Gets the Base Failure Rate (BFR) for the given scope.
        /// </summary>
        /// <returns>The base failure rate for scope.  0 if this module only implements Momentary Failure Rates</returns>
        /// <param name="flightData">The flight data that failure rate should be calculated on.</param>
        double GetBaseFailureRate(float flightData);

        /// <summary>
        /// Gets the reliability curve for the given scope.
        /// </summary>
        /// <returns>The reliability curve for scope.  MUST return null if the reliability module does not handle Base Failure Rate</returns>
        FloatCurve GetReliabilityCurve();

        /// <summary>
        /// Gets the current burn time for the given scope.
        /// </summary>
        /// <returns>The current burn time for scope or 0 if this module does not track burn time.</returns>
        float GetScopedRunTime(RatingScope ratingScope);
 
        /// <summary>
        /// Should return a string if the module wants to report any information to the user in the TestFlight Editor window.
        /// </summary>
        /// <returns>A string of information to display to the user, or "" if none</returns>
        /// <param name="reliabilityAtTime">The time at which to give indicative reliability stats at.</param>
        List<string> GetTestFlightInfo(float reliabilityAtTime);

        /// <summary>
        /// Should return a string if the module wants to report any information to the user in the stock part info panel.
        /// </summary>
        /// <returns>A string of information to display to the user, or "" if none</returns>
        /// <param name="reliabilityAtTime">The time at which to give indicative reliability stats at.</param>
        string GetModuleInfo(string configuration, float reliabilityAtTime);

        /// <summary>
        /// Should return a float if the module has data about a rated use or cycle time (such as engine burn times)
        /// </summary>
        /// <returns>A float with time in seconds, or 0f if none</returns>
        float GetRatedTime(string configuration, RatingScope ratingScope);
        float GetRatedTime(RatingScope ratingScope);

        void SetActiveConfig(string configuration);
    }

    public interface ITestFlightFailure
    {
        bool Failed
        {
            get;
            set;
        }

        bool TestFlightEnabled
        {
            get;
        }

        string Configuration
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the details of the failure encapsulated by this module.  In most cases you can let the base class take care of this unless oyu need to do somethign special
        /// </summary>
        /// <returns>The failure details.</returns>
        TestFlightFailureDetails GetFailureDetails();

        /// <summary>
        /// Triggers the failure controlled by the failure module
        /// </summary>
        void DoFailure();

        /// <summary>
        /// Forces the repair.  This should instantly repair the part, regardless of whether or not a normal repair can be done.  IOW if at all possible the failure should fixed after this call.
        /// This is made available as an API method to allow things like failure simulations.
        /// </summary>
        /// <returns>The seconds until repair is complete, <c>0</c> if repair is completed instantly, and <c>-1</c> if repair failed and the part is still broken.</returns>
        float ForceRepair();

        float GetRepairTime();
        float AttemptRepair();
        bool CanAttemptRepair();

        /// <summary>
        /// Should return a string if the module wants to report any information to the user in the TestFlight Editor window.
        /// </summary>
        /// <returns>A string of information to display to the user, or "" if none</returns>
        List<string> GetTestFlightInfo();

        /// <summary>
        /// Should return a string if the module wants to report any information to the user in the stock part info panel.
        /// </summary>
        /// <returns>A string of information to display to the user, or "" if none</returns>
        string GetModuleInfo(string configuration);
        
        void SetActiveConfig(string configuration);
    }

    public interface ITestFlightInterop
    {
        bool AddInteropValue(string name, int value, string owner);

        bool AddInteropValue(string name, float value, string owner);

        bool AddInteropValue(string name, bool value, string owner);

        bool AddInteropValue(string name, string value, string owner);

        bool RemoveInteropValue(string name, string owner);

        void ClearInteropValues(string owner);

        InteropValue GetInterop(string name);
    }

    /// <summary>
    /// This is used internally and should not be implemented by any 3rd party modules
    /// </summary>
    public interface ITestFlightCore
    {
        bool ActiveConfiguration
        {
            get;
        }
        bool TestFlightEnabled
        {
            get;
        }

        string Configuration
        {
            get;
            set;
        }

        string Alias
        {
            get;
        }

        string Title
        {
            get;
        }

        System.Random RandomGenerator
        {
            get;
        }

        bool DebugEnabled
        {
            get;
        }

        /// <summary>
        /// 0 = OK, 1 = Has Failure
        /// </summary>
        /// <returns>The part status.</returns>
        int GetPartStatus();

        // NEW noscope based as of v1.3
        void InitializeFlightData(float flightData);

        void HighlightPart(bool doHighlight);

        // Get the base or static failure rate
        double GetBaseFailureRate();

        float GetMaximumData();
        // Get the Reliability Curve for the part
        FloatCurve GetBaseReliabilityCurve();
        // Get the momentary (IE current dynamic) failure rates (Can vary per reliability/failure modules)
        // These  methods will let you get a list of all momentary rates or you can get the best (lowest chance of failure)/worst (highest chance of failure) rates
        // Note that the return value is alwasy a dictionary.  The key is the name of the trigger, always in lowercase.  The value is the failure rate.
        // The dictionary will be a single entry in the case of Worst/Best calls, and will be the length of total triggers in the case of askign for All momentary rates.
        MomentaryFailureRate GetWorstMomentaryFailureRate();

        MomentaryFailureRate GetBestMomentaryFailureRate();

        List<MomentaryFailureRate> GetAllMomentaryFailureRates();

        double GetMomentaryFailureRateForTrigger(String trigger);
        // The momentary failure rate is tracked per named "trigger" which allows multiple Reliability or FailureTrigger modules to cooperate
        // Returns the total modified failure rate back to the caller for convenience
        double SetTriggerMomentaryFailureModifier(String trigger, double multiplier, PartModule owner);
        // simply converts the failure rate into a MTBF string.  Convenience method
        // Returned string will be of the format "123.00 units"
        // Optionally specify a maximum size for MTBF.  If the given units would return an MTBF larger than maximu, it will
        // automaticly be converted into progressively higher units until the returned value is <= maximum
        String FailureRateToMTBFString(double failureRate, TestFlightUtil.MTBFUnits units);

        String FailureRateToMTBFString(double failureRate, TestFlightUtil.MTBFUnits units, int maximum);
        // Short version of MTBFString uses a single letter to denote (s)econds, (m)inutes, (h)ours, (d)ays, (y)ears
        // So the returned string will be EF "12.00s" or "0.20d"
        // Optionally specify a maximum size for MTBF.  If the given units would return an MTBF larger than maximu, it will
        // automaticly be converted into progressively higher units until the returned value is <= maximum
        String FailureRateToMTBFString(double failureRate, TestFlightUtil.MTBFUnits units, bool shortForm);

        String FailureRateToMTBFString(double failureRate, TestFlightUtil.MTBFUnits units, bool shortForm, int maximum);
        void FailureRateToMTBFString(double failureRate, TestFlightUtil.MTBFUnits units, bool shortForm, int maximum, out string output);
        // Simply converts the failure rate to a MTBF number, without any string formatting
        double FailureRateToMTBF(double failureRate, TestFlightUtil.MTBFUnits units);
        // Get the FlightData or FlightTime for the part
        float GetFlightData();

        float GetInitialFlightData();

        float GetFlightTime();
        // Methods to restrict the amount of data accumulated.  Useful for KCT or other "Simulation" mods to use
        float SetDataRateLimit(float limit);

        float SetDataCap(float cap);
        // Set the FlightData for FlightTime or the part - this is an absolute set and replaces the previous FlightData/Time
        // This is generally NOT recommended.  Use ModifyFlightData instead so that the Core can ensure your modifications cooperate with others
        // These functions are currently NOT implemented!
        void SetFlightData(float data);

        void SetFlightTime(float seconds);
        // Modify the FlightData or FlightTime for the part
        // The given modifier is multiplied against the current FlightData unless additive is true
        float ModifyFlightData(float modifier);

        float ModifyFlightTime(float modifier);

        float ModifyFlightData(float modifier, bool additive);

        float ModifyFlightTime(float modifier, bool additive);
        // Returns the total engineer bonus for the current vessel's current crew based on the given part's desired per engineer level bonus
        float GetEngineerDataBonus(float partEngineerBonus);
        // Cause a failure to occur, either a random failure or a specific one
        // If fallbackToRandom is true, then if the specified failure can't be found or can't be triggered, a random failure will be triggered instead
        ITestFlightFailure TriggerFailure();

        ITestFlightFailure TriggerNamedFailure(String failureModuleName);

        ITestFlightFailure TriggerNamedFailure(String failureModuleName, bool fallbackToRandom);
        // Returns a list of all available failures on the part
        List<String> GetAvailableFailures();
        // Enable a failure so it can be triggered (this is the default state)
        void EnableFailure(String failureModuleName);
        // Disable a failure so it can not be triggered
        void DisableFailure(String failureModuleName);
        // Returns the Operational Time or the time, in MET, since the last time the part was fully functional.
        // If a part is currently in a failure state, return will be -1 and the part should not fail again
        // This counts from mission start time until a failure, at which point it is reset to the time the
        // failure is repaired.  It is important to understand this is NOT the total flight time of the part.
        float GetOperatingTime();

        /// <summary>
        /// Determines whether the part is considered operating or not.
        /// </summary>
        bool IsPartOperating();

        float GetRunTime(RatingScope ratingScope);

        /// <summary>
        /// Called whenever an Interop value is added, changed, or removed to allow the modules on the part to update to the proper config
        /// </summary>
        void UpdatePartConfig();

        float GetMaximumRnDData();

        float GetRnDCost();

        float GetRnDRate();

        float ForceRepair(ITestFlightFailure failure);

        List<ITestFlightFailure> GetActiveFailures();

        /// <summary>
        /// Should return a string if the module wants to report any information to the user in the TestFlight Editor window.
        /// </summary>
        /// <returns>A string of information to display to the user, or "" if none</returns>
        List<string> GetTestFlightInfo();

        float GetTechTransfer();
    }
}

