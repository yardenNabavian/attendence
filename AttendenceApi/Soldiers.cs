using Google.Cloud.Firestore;
using Google.Apis.Auth.OAuth2;
namespace AttendenceApi;

public class Company 
{
    private Dictionary<Platoon, List<Soldier>> _platoons { get; set; }
    private readonly FirestoreDb _db;

    public Company()
    {
        // Initialize Firestore
        var projectId = Environment.GetEnvironmentVariable("FIREBASE_PROJECT_ID");
        var credentialsJson = Environment.GetEnvironmentVariable("FIREBASE_SERVICE_ACCOUNT");
        if (string.IsNullOrWhiteSpace(projectId) || string.IsNullOrWhiteSpace(credentialsJson))
        {
            throw new InvalidOperationException("FIREBASE_PROJECT_ID or FIREBASE_SERVICE_ACCOUNT environment variables are not set.");
        }

        var credential = Google.Apis.Auth.OAuth2.GoogleCredential.FromJson(credentialsJson);
        _db = new FirestoreDbBuilder
        {
            ProjectId = projectId,
            Credential = credential
        }.Build();

        _platoons = new Dictionary<Platoon, List<Soldier>>
        {
            {Platoon.Platoon1, new List<Soldier>()},
            {Platoon.Platoon2, new List<Soldier>()},
            {Platoon.Command, new List<Soldier>()}
        };

        LoadFromFirestore();
    }

    public bool AddSoldier(Platoon platoon, Soldier soldier)
    {
        if (_platoons.Values.Any(list => list.Any(s => s.Id == soldier.Id)))
        {
            return false;
        }

        _platoons[platoon].Add(soldier);
        SaveSoldierToFirestore(platoon, soldier);
        return true;
    }

    public void RemoveSoldier(Platoon platoon, int soldierId)
    {
        var soldier = _platoons[platoon].FirstOrDefault(s => s.Id == soldierId);
        if (soldier is not null)
        {
            _platoons[platoon].Remove(soldier);
            DeleteSoldierFromFirestore(platoon, soldierId);
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

        UpdateSoldierInFirestore(platoon, soldier);
    }

    public List<Soldier> GetPlatoon(Platoon platoon)
    {
        return _platoons[platoon];
    }

    internal Dictionary<Platoon, List<Soldier>> GetCompany()
    {
        return _platoons;
    }

    private void SaveSoldierToFirestore(Platoon platoon, Soldier soldier)
    {
        var docRef = _db.Collection("platoons").Document(platoon.ToString()).Collection("soldiers").Document(soldier.Id.ToString());
        docRef.SetAsync(soldier).GetAwaiter().GetResult();
    }

    private void DeleteSoldierFromFirestore(Platoon platoon, int soldierId)
    {
        var docRef = _db.Collection("platoons").Document(platoon.ToString()).Collection("soldiers").Document(soldierId.ToString());
        docRef.DeleteAsync().GetAwaiter().GetResult();
    }

    private void UpdateSoldierInFirestore(Platoon platoon, Soldier soldier)
    {
        var docRef = _db.Collection("platoons").Document(platoon.ToString()).Collection("soldiers").Document(soldier.Id.ToString());
        docRef.SetAsync(soldier).GetAwaiter().GetResult();
    }

    private void LoadFromFirestore()
    {
        foreach (var platoon in Enum.GetValues<Platoon>())
        {
            var soldiersSnap = _db.Collection("platoons").Document(platoon.ToString()).Collection("soldiers").GetSnapshotAsync().GetAwaiter().GetResult();
            foreach (var doc in soldiersSnap.Documents)
            {
                var soldier = doc.ConvertTo<Soldier>();
                _platoons[platoon].Add(soldier);
            }
        }
    }
}

[FirestoreData]
public class Soldier
{
    [FirestoreProperty]
    public int Id { get; set; }
    [FirestoreProperty]
    public string? Name { get; set; }
    [FirestoreProperty]
    public Status Status { get; set; }
    [FirestoreProperty]
    public string? Location { get; set; }
    [FirestoreProperty]
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

