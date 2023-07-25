using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace OBDLinkTools;

public enum MeasurementTypes
{
    Deg,
    Mph,
    Mpg,
    GalHr,
    LbMile,
    Lbs,
    LbMin,
    FtSSqr,
    DegS,
    Ut,
    Percent,
    DegF,
    InHg,
    Rpm,
    V,
    Sec,
    Miles,
    InH2O,
    Ma,
    Psi,
    Hp,
    LbFt,
    Gal,
    Min,
    Ft,
    Unknown
}
    
public class DataRecord
{
    public DateTime Time { get; set; }
    [Key]
    public Guid Id { get; set; }
    public string Name { get; set; }
    public MeasurementTypes MeasurementType { get; set; }
    public double Value { get; set; }
}

public abstract class Program
{
    private static List<DataRecord> LoadDataRecordsFromCsv(string filePath)
    {
        var records = new List<DataRecord>();

        // The file format is:
        // First line, discard
        // Second line, headers
        // All other lines, data
        
        using var reader = new StreamReader(filePath);

        // First, skip the first line
        reader.ReadLine();
        
        // Then, read the headers
        var headers = reader.ReadLine()?.Split(',');
        
        // Finally, read the data and create DataRecord objects
        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (line == null) continue;
            
            var values = line.Split(',');
            var time = DateTime.Parse(values[0]);
            
            for (var i = 1; i < values.Length; i++)
            {
                var value = values[i];
                if (value == "NODATA") continue;
                
                var (name, measurementType) = GetNameAndMeasurementType(headers[i]);

                var record = new DataRecord
                {
                    Time = time,
                    Name = name,
                    MeasurementType = measurementType,
                    Value = double.Parse(value)
                };
                
                records.Add(record);
            }
        }

        return records;
    }

    private static MeasurementTypes GetMeasurementTypeFromName(string name)
    {
        return name switch
        {
            not null when name.Contains("(deg)") => MeasurementTypes.Deg,
            not null when name.Contains("(MPH)") => MeasurementTypes.Mph,
            not null when name.Contains("(MPG)") => MeasurementTypes.Mpg,
            not null when name.Contains("(gal/hr)") => MeasurementTypes.GalHr,
            not null when name.Contains("(lb/mile)") => MeasurementTypes.LbMile,
            not null when name.Contains("(lbs)") => MeasurementTypes.Lbs,
            not null when name.Contains("(lb/min)") => MeasurementTypes.LbMin,
            not null when name.Contains("(ft/s²)") => MeasurementTypes.FtSSqr,
            not null when name.Contains("(deg/s)") => MeasurementTypes.DegS,
            not null when name.Contains("(µT)") => MeasurementTypes.Ut,
            not null when name.Contains("(%)") => MeasurementTypes.Percent,
            not null when name.Contains("(°F)") => MeasurementTypes.DegF,
            not null when name.Contains("(inHg)") => MeasurementTypes.InHg,
            not null when name.Contains("(RPM)") => MeasurementTypes.Rpm,
            not null when name.Contains("(V)") => MeasurementTypes.V,
            not null when name.Contains("(sec)") => MeasurementTypes.Sec,
            not null when name.Contains("(miles)") => MeasurementTypes.Miles,
            not null when name.Contains("(inH2O)") => MeasurementTypes.InH2O,
            not null when name.Contains("(mA)") => MeasurementTypes.Ma,
            not null when name.Contains("(psi)") => MeasurementTypes.Psi,
            not null when name.Contains("(hp)") => MeasurementTypes.Hp,
            not null when name.Contains("(lb•ft)") => MeasurementTypes.LbFt,
            not null when name.Contains("(gal)") => MeasurementTypes.Gal,
            not null when name.Contains("(min)") => MeasurementTypes.Min,
            not null when name.Contains("(ft)") => MeasurementTypes.Ft,
            _ => MeasurementTypes.Unknown
        };
    }

    private static Tuple<string, MeasurementTypes> GetNameAndMeasurementType(string name)
    {
        var type = GetMeasurementTypeFromName(name);

        // Remove measurement type from name if it exists
        var cleanName = name;
        if (type == MeasurementTypes.Unknown) return new Tuple<string, MeasurementTypes>(cleanName, type);
        
        var startParenthesisIndex = name.LastIndexOf("(", StringComparison.Ordinal);
        if (startParenthesisIndex != -1)
        {
            cleanName = name[..startParenthesisIndex].Trim();
        }

        return new Tuple<string, MeasurementTypes>(cleanName, type);
    }
    
    private static void CreateDatabaseFromDataRecords(IEnumerable<DataRecord> dataRecords)
    {
        using var context = new LogDataContext();
        
        // This ensures that the database and table are created if they don't exist
        context.Database.EnsureCreated();

        // Add the data records to the LogData table
        context.LogData.AddRange(dataRecords);

        // Save the changes to the database
        context.SaveChanges();
    }

    // Main
    public static void Main(string[] args)
    {
        // 0. Resolve the path "~/Downloads/obd2.csv"
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var filePath = Path.Combine(homeDirectory, "Downloads", "obd2.csv");
        
        // 1. Load ~/Downloads/obd2.csv into a List<DataRecord>
        var records = LoadDataRecordsFromCsv(filePath);
        
        // 2. Group the records by time
        var groupedRecords = records.GroupBy(record => record.Time);
        
        // 3. Create the database from the grouped records
        CreateDatabaseFromDataRecords(records);
    }
}

public class LogDataContext : DbContext
{
    public DbSet<DataRecord> LogData { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Filename=LogData.db");
    }
}