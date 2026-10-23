#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;

namespace PowerSimBuilder.Physics;

/// <summary>
/// Reads FMU metadata (modelDescription.xml) into an <see cref="FmuInfo"/>.
/// Extends PhysicsImportForm.ParseFmu with modelName, fmiVersion,
/// parameter start values, and DefaultExperiment timing.
/// </summary>
public static class FmuInfoReader
{
    public static FmuInfo Read(string fmuPath)
    {
        if (!File.Exists(fmuPath))
            throw new FileNotFoundException("FMU not found", fmuPath);

        using var zip = ZipFile.OpenRead(fmuPath);
        var entry = zip.GetEntry("modelDescription.xml")
            ?? throw new InvalidDataException(
                "modelDescription.xml not found. Not a valid .fmu archive.");

        using var stream = entry.Open();
        var doc = XDocument.Load(stream);
        var root = doc.Root
            ?? throw new InvalidDataException("Empty modelDescription.xml");

        var modelName  = (string?)root.Attribute("modelName")  ?? "";
        var fmiVersion = (string?)root.Attribute("fmiVersion") ?? "";
        var fmiType    = (string?)root.Attribute("fmiType")    ?? GuessFmiType(root);

        var inputs     = new List<string>();
        var outputs    = new List<string>();
        var parameters = new List<FmuParameter>();

        var modelVariables = root.Elements()
            .FirstOrDefault(e => e.Name.LocalName == "ModelVariables");

        if (modelVariables != null)
        {
            foreach (var sv in modelVariables.Elements()
                .Where(e => e.Name.LocalName == "ScalarVariable"))
            {
                var name = (string?)sv.Attribute("name") ?? "";
                if (string.IsNullOrEmpty(name)) continue;
                var causality = (string?)sv.Attribute("causality") ?? "local";

                switch (causality)
                {
                    case "input":
                        inputs.Add(name);
                        break;
                    case "output":
                        outputs.Add(name);
                        break;
                    case "parameter":
                        parameters.Add(new FmuParameter(name, ReadStartValue(sv)));
                        break;
                }
            }
        }

        return new FmuInfo(
            modelName,
            fmiVersion,
            fmiType,
            inputs,
            outputs,
            parameters,
            ReadTiming(root));
    }

    private static string ReadStartValue(XElement scalarVariable)
    {
        var typed = scalarVariable.Elements().FirstOrDefault();
        if (typed == null) return "0";

        var typeName = typed.Name.LocalName;
        var start = (string?)typed.Attribute("start");

        if (typeName == "Real" || typeName == "Integer")
            return string.IsNullOrEmpty(start) ? "0" : start!;

        if (typeName == "Boolean")
            return start == "true" ? "True" : "False";

        if (typeName == "String")
            return start == null ? "\"\"" : "\"" + start.Replace("\"", "\\\"") + "\"";

        // Enumeration and unsupported types -> caller decides (TODO).
        return "# TODO";
    }

    private static FmuTiming? ReadTiming(XElement root)
    {
        var exp = root.Elements()
            .FirstOrDefault(e => e.Name.LocalName == "DefaultExperiment");
        if (exp == null) return null;

        double? Parse(string attr)
        {
            var raw = (string?)exp.Attribute(attr);
            if (string.IsNullOrEmpty(raw)) return null;
            return double.TryParse(raw, NumberStyles.Float,
                CultureInfo.InvariantCulture, out var v) ? v : (double?)null;
        }

        var start = Parse("startTime");
        var stop  = Parse("stopTime");
        var step  = Parse("stepSize");
        if (start == null && stop == null && step == null) return null;
        return new FmuTiming(start, stop, step);
    }

    private static string GuessFmiType(XElement root)
    {
        if (root.Descendants().Any(e => e.Name.LocalName == "CoSimulation"))
            return "CoSimulation";
        if (root.Descendants().Any(e => e.Name.LocalName == "ModelExchange"))
            return "ModelExchange";
        return "CoSimulation";
    }
}