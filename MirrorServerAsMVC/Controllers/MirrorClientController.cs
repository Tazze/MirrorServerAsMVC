using Microsoft.ApplicationInsights;
using Microsoft.AspNet.Identity.EntityFramework;
using MirrorServerAsMVC.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace MirrorServerAsMVC.Controllers
{
    public class MirrorClientController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<JsonResult> GetUserInfoStream()
        {
            createReceivedDirectory(Server);

            var stream = Request.InputStream;
            FileStream fileStream = null;
            try
            {
                fileStream = System.IO.File.Create(Server.MapPath("~/Received/stream.jpg"));
            }catch(Exception ex)
            {
                new TelemetryClient().TrackException(ex);
            }
            stream.Seek(0, SeekOrigin.Begin);
            stream.CopyTo(fileStream);
            fileStream.Close();


            string accessToken = await CheckUserIdentity();
            return Json(new UserDataEntity(GetUserTasks(accessToken), GetUserCalendarEvents(accessToken), GetUserEmails(accessToken)));

        }

        private static void createReceivedDirectory(HttpServerUtilityBase Server)
        {
            // Specify the directory you want to manipulate.
            string path = Server.MapPath("~/Received");

            try
            {
                // Determine whether the directory exists.
                if (Directory.Exists(path))
                {
                    //Console.WriteLine("That path exists already.");
                    return;
                }

                // Try to create the directory.
                DirectoryInfo di = Directory.CreateDirectory(path);
                //Console.WriteLine("The directory was created successfully at {0}.", Directory.GetCreationTime(path));
            }
            catch (Exception e)
            {
                new TelemetryClient().TrackException(e);
            }
            finally { }
        }

        static string OxfordKey = "";
        static string detectUrl = "https://api.projectoxford.ai/face/v1.0/detect?returnFaceId=true&returnFaceLandmarks=false&subscription-key=" + OxfordKey;
        static string verifyUrl = "https://api.projectoxford.ai/face/v1.0/verify&subscription-key=" + OxfordKey;

        private async static Task<string> CheckUserIdentity()
        {
            //TODO: create FaceId for image
            //if there's a face, beging verifying process
            //if faceId to check against is nonexistent or expired, create a new faceId.
            //if match is found, return id, else return empty string.

            //var context = new ApplicationDbContext();
            //var users = context.Users;
            //JToken token = await postAndReturn(detectUrl, new DetectDataEntity("https://mirrorserverasmvc20160613125538.azurewebsites.net/Received/Stream.jpg"));
            //var streamFaceId = (string)token.SelectToken("faceId");
            //foreach (ApplicationUser user in users)
            //{
            //    if (await Verify(user, streamFaceId))
            //    {
            //        foreach(IdentityUserLogin login in user.Logins)
            //        {
            //            return login.ProviderKey;
            //        }
            //    }
            //}
            return "";
        }


        private async static Task<bool> Verify(ApplicationUser user, string streamFaceId)
        {
            //https://api.projectoxford.ai/face/v1.0/detect?returnFaceId=true&returnFaceLandmarks=false&subscription-key= <Your subscription key>
            //+ JSON of url
            // example:
            //"url":"http://example.com/1.jpg"

            //https://api.projectoxford.ai/face/v1.0/verify&subscription-key= <Your subscription key>
            //+ JSON of two face id
            //example:
            //"faceId1":"c5c24a82-6845-4031-9d5d-978df9175426",
            //"faceId2":"015839fb-fbd9-4f79-ace9-7675fc2f1dd9"

            if (DateTime.SpecifyKind(user.LastUpdate, DateTimeKind.Utc).AddHours(24) < DateTime.UtcNow)
            {
                //update FaceId
                user.LastUpdate = DateTime.Now;
                JToken detectToken = await postAndReturn(detectUrl, new DetectDataEntity(user.PhotoUrl));
                user.FaceId = (string)detectToken.SelectToken("faceId");
            }
            //check stream against user

            JToken token = await postAndReturn(verifyUrl, new VerifyDataEntity(user.FaceId, streamFaceId));

            return (bool)token.SelectToken("isIdentical");
        }

        private async static Task<JToken> postAndReturn(string url, object data)
        {
            var client = new HttpClient();
            var response = await client.PostAsync(url, new StringContent(data.ToString(),Encoding.UTF8,"application/json"));

            var responseString = await response.Content.ReadAsStringAsync();

            return JObject.Parse(responseString); ;
        }

        private static UserTaskEntity[] GetUserTasks(string id)
        {
            UserTaskEntity[] result = new UserTaskEntity[1];
            result[0] = new UserTaskEntity();
            return result;
        }

        private static UserCalendarEventEntity[] GetUserCalendarEvents(string id)
        {
            UserCalendarEventEntity[] result = new UserCalendarEventEntity[1];
            result[0] = new UserCalendarEventEntity();
            return result;
        }

        private static UserEmailEntity[] GetUserEmails(string id)
        {
            UserEmailEntity[] result = new UserEmailEntity[1];
            result[0] = new UserEmailEntity();
            return result;
        }
    }
    public class VerifyDataEntity
    {
        public string faceId1;
        public string faceId2;

        public VerifyDataEntity(string fid1, string fid2)
        {
            faceId1 = fid1;
            faceId2 = fid2;
        }
    }
    public class DetectDataEntity
    {
        public string url;

        public DetectDataEntity(string urlin)
        {
            url = urlin;
        }
    }
    public class UserDataEntity
    {
        public UserTaskEntity[] tasks;
        public UserCalendarEventEntity[] calendarEvents;
        public UserEmailEntity[] emails;

        public UserDataEntity(UserTaskEntity[] tasksIn, UserCalendarEventEntity[] calendarEventsIn, UserEmailEntity[] emailsIn)
        {
            tasks = tasksIn;
            calendarEvents = calendarEventsIn;
            emails = emailsIn;
        }

    }
    public class UserTaskEntity
    {
        public UserTaskEntity()
        {
            task = "this is a test task";
        }

        public string task;
    }
    public class UserCalendarEventEntity
    {

        public UserCalendarEventEntity()
        {
            calendarEvent = "this is a test calendarEvent";
            var now = DateTime.Now;
            time = now.ToLongDateString() + " " + now.ToLongTimeString();
            location = "here";
        }

        public string calendarEvent;
        public string time;
        public string location;
    }
    public class UserEmailEntity
    {
        public UserEmailEntity()
        {
            sender = "this is a test sender";
            subject = "test";
        }

        public string sender;
        public string subject;
    }
}