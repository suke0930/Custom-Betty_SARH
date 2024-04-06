using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using Harmony;
using System.Reflection;

public class CustomBetty2 : VTOLMOD
{
    public enum CommonWarnings2
    {
        SARHLockBlip,
    }


    public class Profile
    {
        public string filePath;
        public string name;
        public List<LineGroup> lineGroups;

        public AudioClip blip;
        public AudioClip irMissileIncoming;
        public AudioClip lockBlip;
        public AudioClip missileLoopLock;
        public AudioClip newContactBlip;
        public AudioClip Shoot;
        public AudioClip collisionWarning;
        public AudioClip stallWarning;

        public FlightWarnings.CommonWarningsClips GenerateBettyVoiceProfile()
        {
            FlightWarnings.CommonWarningsClips output = new FlightWarnings.CommonWarningsClips();
            for (int i = 0; i < lineGroups.Count; i++)
            {
                AudioClip temp = lineGroups[i].GenerateMessageAudio();
                switch (lineGroups[i].type)
                {
                    case CommonWarnings2.SARHLockBlip:
                        collisionWarning = temp;
                        break;
                    default:
                        break;
                }
            }
            return output;
        }

        public void GetFilePaths()
        {
            lineGroups = new List<LineGroup>();

            Debug.Log("Checking for: " + filePath);

            if (Directory.Exists(filePath))
            {
                Debug.Log(filePath + " exists!");
                DirectoryInfo info = new DirectoryInfo(filePath);
                foreach (CommonWarnings2 messageType in Enum.GetValues(typeof(CommonWarnings2)))
                {
                    Debug.Log("Checking for: " + messageType.ToString());
                    if (Directory.Exists(filePath + messageType.ToString() + @"\"))
                    {
                        Debug.Log("Found: " + messageType.ToString());
                        LineGroup temp = new LineGroup();
                        temp.filePath = filePath + messageType.ToString() + @"\";
                        temp.type = messageType;
                        temp.GetFilePaths();
                        lineGroups.Add(temp);
                        Debug.Log("\n");
                    }
                    else
                    {
                        Debug.Log(filePath + messageType.ToString() + " doesn't exist, please add it or the voicepack will not work as intended.");
                    }
                }
            }
            else
            {
                Debug.Log(filePath + " doesn't exist.");
            }
        }
    }

    public class LineGroup
    {
        public string filePath;
        public CommonWarnings2 type;
        public string clipPath;
        public AudioClip clip;

        public AudioClip GenerateMessageAudio()
        {
            return clip;
        }

        public void GetFilePaths()
        {
            Debug.Log("Checking for: " + filePath);

            if (Directory.Exists(filePath))
            {
                Debug.Log(filePath + " exists!");
                DirectoryInfo info = new DirectoryInfo(filePath);
                foreach (FileInfo item in info.GetFiles("*.wav"))
                {
                    Debug.Log("Found line: " + item.Name);
                    clipPath = filePath + item.Name;
                    Debug.Log("\n");
                }
            }
            else
            {
                Debug.Log(filePath + " doesn't exist.");
            }
        }
    }

    public static CustomBetty2 instance;

    public string address;
    public List<Profile> profiles;
    public List<FlightWarnings.CommonWarningsClips> bettyVoiceProfiles;

    public int currentProfileID;
    public Profile currentProfile;
    public FlightWarnings.CommonWarningsClips currentCommonWarnings;

    public override void ModLoaded()
    {
        HarmonyInstance harmony = HarmonyInstance.Create("cheese.customBettyARH");
        harmony.PatchAll(Assembly.GetExecutingAssembly());

        base.ModLoaded();

        bettyVoiceProfiles = new List<FlightWarnings.CommonWarningsClips>();
        VTOLAPI.SceneLoaded += SceneLoaded;
        VTOLAPI.MissionReloaded += MissionReloaded;

        LoadCustomWingmen();

        instance = this;
    }

    void SceneLoaded(VTOLScenes scene)
    {
        switch (scene)
        {
            case VTOLScenes.Akutan:
            case VTOLScenes.CustomMapBase:
                StartCoroutine("SetupScene");
                break;
            case VTOLScenes.VehicleConfiguration:
                if (bettyVoiceProfiles.Count > 0)
                {
                    Debug.Log("Replacing betty!");
                    currentProfileID = UnityEngine.Random.Range(0, bettyVoiceProfiles.Count);
                    currentProfile = profiles[currentProfileID];
                    currentCommonWarnings = bettyVoiceProfiles[currentProfileID];
                }
                else
                {
                    Debug.Log("There are no betty voice packs, cannot replace betty...");
                }
                break;
            default:
                break;
        }
    }

    private void MissionReloaded()
    {
        StartCoroutine("SetupScene");
    }

    private IEnumerator SetupScene()
    {
        while (VTMapManager.fetch == null || !VTMapManager.fetch.scenarioReady || FlightSceneManager.instance.switchingScene)
        {
            yield return null;
        }
    }

    private void LoadCustomWingmen()
    {
        profiles = new List<Profile>();

        address = Directory.GetCurrentDirectory() + @"\VTOLVR_ModLoader\mods\";
        Debug.Log("Checking for: " + address);

        if (Directory.Exists(address))
        {
            Debug.Log(address + " exists!");
            DirectoryInfo info = new DirectoryInfo(address);
            foreach (DirectoryInfo item in info.GetDirectories())
            {
                try
                {
                    Debug.Log("Checking for: " + address + item.Name + @"\bettyvoiceinfo.txt");
                    string temp = File.ReadAllText(address + item.Name + @"\bettyvoiceinfo.txt");
                    Debug.Log("Found betty voice pack: " + temp);

                    Profile tempProfile = new Profile();
                    tempProfile.name = temp;
                    tempProfile.filePath = address + item.Name + @"\";
                    tempProfile.GetFilePaths();
                    profiles.Add(tempProfile);
                }
                catch
                {
                    Debug.Log(item.Name + " is not an Betty voice pack.");
                }
                Debug.Log("\n");
            }
        }
        else
        {
            Debug.Log(address + " doesn't exist.");
        }
        Debug.Log("Loading audioClips");
        StartCoroutine(LoadAudioFile());
    }

    private void MakeBettyVoiceProfiles()
    {
        for (int i = 0; i < profiles.Count; i++)
        {
            bettyVoiceProfiles.Add(profiles[i].GenerateBettyVoiceProfile());
        }
    }

    IEnumerator LoadAudioFile()
    {
        for (int y = 0; y < profiles.Count; y++)
        {
            for (int x = 0; x < profiles[y].lineGroups.Count; x++)
            {
                using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(profiles[y].lineGroups[x].clipPath, AudioType.WAV))
                {
                    yield return www.Send();

                    if (www.isNetworkError)
                    {
                        Debug.Log(www.error);
                    }
                    else
                    {
                        profiles[y].lineGroups[x].clip = DownloadHandlerAudioClip.GetContent(www);
                    }
                }
                Debug.Log("Loaded " + profiles[y].lineGroups[x].clipPath);
            }
        }
        MakeBettyVoiceProfiles();
    }
}