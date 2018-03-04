using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
#if !UNITY_EDITOR
using System.Net.Http;
using System.Net.Http.Headers;
#endif
using UnityEngine;
using UnityEngine.Windows.Speech;
using UnityEngine.XR.WSA.Input;
using UnityEngine.XR.WSA.WebCam;
public class GazeGestureManager : MonoBehaviour
{
    private string filePath;
    public static GazeGestureManager Instance { get; private set; }

    // Represents the hologram that is currently being gazed at.
    public GameObject FocusedObject { get; private set; }

    GestureRecognizer recognizer;

    // Use this for initialization
    void Start() {
        List<string> keywords = new List<string>();
        keywords.Add("Take Picture");
        keywordRecognizer = new KeywordRecognizer(keywords.ToArray());
        keywordRecognizer.OnPhraseRecognized += KeywordRecognizer_OnPhraseRecognized;
        keywordRecognizer.Start();
        Debug.Log("Started");

    }
    private void KeywordRecognizer_OnPhraseRecognized(PhraseRecognizedEventArgs args) {
        Debug.Log("Listening..");
        PhotoCapture.CreateAsync(false, OnPhotoCaptureCreated);
    }
    /*void Awake()
    {
        Instance = this;

        // Set up a GestureRecognizer to detect Select gestures.
        recognizer = new GestureRecognizer();
        recognizer.Tapped += (args) =>
        {
            Debug.Log("Air Tap Gesture");
            
            //ProcessPhotoAsync();

            // Send an OnSelect message to the focused object and its ancestors.
            if (FocusedObject != null)
            {
                FocusedObject.SendMessageUpwards("OnSelect", SendMessageOptions.DontRequireReceiver);
            }
        };
        recognizer.StartCapturingGestures();
    }*/

    // Update is called once per frame
    void Update()
    {
        /*
        // Figure out which hologram is focused this frame.
        GameObject oldFocusObject = FocusedObject;

        // Do a raycast into the world based on the user's
        // head position and orientation.
        var headPosition = Camera.main.transform.position;
        var gazeDirection = Camera.main.transform.forward;

        RaycastHit hitInfo;
        if (Physics.Raycast(headPosition, gazeDirection, out hitInfo))
        {
            // If the raycast hit a hologram, use that as the focused object.
            FocusedObject = hitInfo.collider.gameObject;
        }
        else
        {
            // If the raycast did not hit a hologram, clear the focused object.
            FocusedObject = null;
        }

        // If the focused object changed this frame,
        // start detecting fresh gestures again.
        if (FocusedObject != oldFocusObject)
        {
            recognizer.CancelGestures();
            recognizer.StartCapturingGestures();
        }*/
    }
    PhotoCapture _photoCaptureObject = null;
    private KeywordRecognizer keywordRecognizer;

    void OnPhotoCaptureCreated(PhotoCapture captureObject)
    {
        _photoCaptureObject = captureObject;

        Resolution cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();

        CameraParameters c = new CameraParameters();
        c.hologramOpacity = 0.0f;
        c.cameraResolutionWidth = cameraResolution.width;
        c.cameraResolutionHeight = cameraResolution.height;
        c.pixelFormat = CapturePixelFormat.BGRA32;

        captureObject.StartPhotoModeAsync(c, OnPhotoModeStarted);
    }

    private void OnPhotoModeStarted(PhotoCapture.PhotoCaptureResult result)
    {
        if (result.success)
        {

            string filename = string.Format(@"terminator_analysis.jpg");
            filePath = System.IO.Path.Combine(Application.persistentDataPath, filename);

            //Hello.text = filePath;
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            _photoCaptureObject.TakePhotoAsync(filePath, PhotoCaptureFileOutputFormat.JPG, OnCapturedPhotoToDiskAsync);

        }
        else
        {
            Debug.LogError("Unable to start photo mode!");
        }
    }
    void OnCapturedPhotoToDiskAsync(PhotoCapture.PhotoCaptureResult result)
    {
        if (result.success)
        {
            Debug.Log("Saved Photo to disk!");
            _photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
            #if !UNITY_EDITOR
            var cameraRollFolder = Windows.Storage.KnownFolders.CameraRoll.Path;
            Debug.Log(cameraRollFolder);
            string p = cameraRollFolder + "/terminator_analysis.jpg";
            if (File.Exists(p))
            {
                File.Delete(p);
            }
            File.Move(filePath, Path.Combine(cameraRollFolder, "terminator_analysis.jpg"));
            #endif
        }
        else
        {
            Debug.Log("Failed to save Photo to disk");
        }

        _photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
    }
    void OnStoppedPhotoMode(PhotoCapture.PhotoCaptureResult result)
    {
        _photoCaptureObject.Dispose();
        _photoCaptureObject = null;
    }

    async void ProcessPhotoAsync(){
#if !UNITY_EDITOR
        Windows.Storage.StorageFolder storageFolder = Windows.Storage.KnownFolders.CameraRoll;
        Windows.Storage.StorageFile file = await storageFolder.GetFileAsync("terminator_analysis.jpg");
        if (file != null)
        {
            // Application now has read/write access to the picked file
            //this.textBlock.Text = "Picked photo: " + file.Name;
            Debug.Log("File exists!!");
        }
        else
        {
            //this.textBlock.Text = "Operation cancelled.";
            Debug.Log("File does not exists!!");
        }


        //convert filestream to byte array
        byte[] fileBytes;
        using (var fileStream = await file.OpenStreamForReadAsync())
        {
            var binaryReader = new BinaryReader(fileStream);
            fileBytes = binaryReader.ReadBytes((int)fileStream.Length);
        }

        HttpClient client = new HttpClient();
        client.BaseAddress = new Uri("http://127.0.0.1:5000/");
        MultipartFormDataContent form = new MultipartFormDataContent();
        HttpContent content = new StringContent("file");
        form.Add(content, "file");
        var stream = await file.OpenStreamForReadAsync();
        content = new StreamContent(stream);
        content.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
        {
            Name = "file",
            FileName = file.Name
        };
        form.Add(content);
        var response = await client.PostAsync("facerec", form);
        //textBlock.Text = response.Content.ReadAsStringAsync().Result;
        Debug.Log(response.Content.ReadAsStringAsync().Result);
#endif
    }

}