using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using System.Linq;
namespace AttendenceApi;

public class Company 
{
    private Dictionary<Platoon, List<Soldier>> _platoons { get; set; }
    private readonly string _filePath;

    public Company()
    {
        _filePath = Path.Combine(AppContext.BaseDirectory, "company.json");

        _platoons = new Dictionary<Platoon, List<Soldier>>
        {
            {Platoon.Platoon1, new List<Soldier>()},
            {Platoon.Platoon2, new List<Soldier>()},
            {Platoon.Command, new List<Soldier>()}
        };

        LoadFromFile();
    }

    public bool AddSoldier(Platoon platoon, Soldier soldier)
    {
        // Ensure ID is unique across all platoons
        if (_platoons.Values.Any(list => list.Any(s => s.Id == soldier.Id)))
        {
            return false;
        }

        _platoons[platoon].Add(soldier);
        SaveToFile();
        return true;
    }

    public void RemoveSoldier(Platoon platoon, int soldierId)
    {
        var soldier = _platoons[platoon].FirstOrDefault(s => s.Id == soldierId);
        if (soldier is not null)
        {
            _platoons[platoon].Remove(soldier);
            SaveToFile();
        }
    }

    public void SetSoldierStatus(Platoon platoon, int soldierId, Status status, string location, string? notes = null)
    {
        var soldier = _platoons[platoon].FirstOrDefault(s => s.Id == soldierId);
        if (soldier is null)
        {
            return;
        }

        soldier.Status = status;
        soldier.Location = location;
        if (notes is not null)
        {
            soldier.Notes = notes;
        }

        SaveToFile();
    }

    public List<Soldier> GetPlatoon(Platoon platoon)
    {
        return _platoons[platoon];
    }

    internal Dictionary<Platoon, List<Soldier>> GetCompany()
    {
        return _platoons;
    }

    private void SaveToFile()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        var json = JsonSerializer.Serialize(_platoons, options);
        File.WriteAllText(_filePath, json);
    }

    private void LoadFromFile()
    {
        if (!File.Exists(_filePath))
            return;

        try
        {
            var json = File.ReadAllText(_filePath);
            var options = new JsonSerializerOptions
            {
                Converters = { new JsonStringEnumConverter() }
            };

            var data = JsonSerializer.Deserialize<Dictionary<Platoon, List<Soldier>>>(json, options);
            if (data is not null)
            {
                _platoons = data;
            }
        }
        catch
        {
            // If deserialization fails, ignore and keep default empty structure
        }
    }
}

public class Soldier
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public Status Status { get; set; }
    public string? Location { get; set; }
    public string? Notes { get; set; }
}

public enum Platoon
{
    Platoon1,
    Platoon2,
    Command
}

public enum Status
{
    Present = 1, //נוכח
    Course = 2, //קורס
    Sick = 3, //גימלים
    Break = 4, //חופשה
    LeavingToBreak = 5, //יציאה חופשה
    ReturningFromBreak = 6, //חוזר מחופשה
    UnregisteredLeave = 7, //פיצול
    FinishedService = 8, //סיים שמפ
    NotEnlisted = 9, //לא התייצב
}

