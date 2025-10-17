# Desktop Task Aid

Brightspace Team 6 

Members: <br>
Pierre Allard 300102131 palla036@uottawa.ca<br>
Taha Talha 300264187 ttalh092@uottawa.ca<br>
Yassine Chaouch 300219230 ychao035@uottawa.ca<br>

Repository Link:<br>
https://github.com/Pannvvii/desktop-helper

# Client: 
Name: Lance Nickel <br>
Affiliation/Position: Freshy Sites, WordPress Technician <br>
Client Email: lance@lanickel.com <br>

# Project Overview:

The desktop aid is an application layered on top of a desktop that helps users with ADHD or other attentive disorders to stay on task or be reminded of deadlines. It will include a visual element that is capable of interacting with the desktop environment to some capacity (i.e. dragging the mouse or appearing in front of the active window) with the goal of getting the user's attention for user specified tasks at user specified times. Depending on client specifications, the application may also interact with a previously existing scheduling application to automatically populate the task list.

# Objectives
Personal task list local storage with names, descriptions, and due dates<br>
Interactive desktop element capable of referencing task list<br>
UI capable of adding removing, and editing tasks<br>
Integration with either google calendar (ideal) or windows integrated calendar (fallback) capable of automatically populating the task list<br>

# Expected/Anticipated Architecture
C# .Net<br>
WPF<br>
JSON<br>
Calendar Api<br>

# Anticipated Risks
Accessing the data of a third party calendar app may be difficult depending on how that third party is designed, and what restrictions are placed on that information.<br>
Visual issues stemming from a variety of potential user setups may arise. Various combinations of monitors, refresh rates, and resolutions could cause unforeseen consequences.<br>

# Legal and Social Issues
Users task data privacy is an issue that must be considered. <br>
All media, designs and code must not breach and avoid infringement.<br>

# First Release Plans
The first release will include a storage system for the task list and a placeholder desktop image that is capable of very basic changes based on the contents of the task list. <br>

# Tool Setup
Github version control<br>
Visual Studio<br>

## Testing the Google Calendar Import Feature

Follow these steps to verify the Google Calendar import workflow that was introduced in the latest updates:

1. **Prepare Google API credentials**
   - Create a Google Cloud project (or reuse an existing one) and enable the *Google Calendar API*.
   - Configure an OAuth consent screen for an external desktop application and add the `https://www.googleapis.com/auth/calendar.readonly` scope.
   - Create *OAuth client ID* credentials with the application type set to **Desktop app** and download the JSON file.
   - Open the downloaded JSON in a text editor and confirm it contains an `installed` object with `client_id`, `client_secret`, `token_uri`, and `redirect_uris` fields. If you received credentials like:

     ```json
     {
       "installed": {
         "client_id": "your-client-id.apps.googleusercontent.com",
         "project_id": "your-project-id",
         "auth_uri": "https://accounts.google.com/o/oauth2/auth",
         "token_uri": "https://oauth2.googleapis.com/token",
         "auth_provider_x509_cert_url": "https://www.googleapis.com/oauth2/v1/certs",
         "client_secret": "your-client-secret",
         "redirect_uris": [
           "http://localhost"
         ]
       }
     }
     ```

     copy the entire contents into a file named `google-credentials.json`. Keep this file private—do not commit it to source control.
   - Place `google-credentials.json` next to the desktop helper executable (or in the project root while debugging in Visual Studio). If you skip this step, the import button now opens a file picker so you can locate the JSON and the app will copy it into place for you. The app creates a `GoogleCalendarTokens` directory beside this file to store refresh tokens.

2. **Restore packages and build the app**
   - Open `DesktopHelper.sln` in Visual Studio (2019 or later).
   - When the solution loads, Visual Studio prompts to **Restore** missing packages—accept the prompt or right-click the solution in Solution Explorer and choose **Restore NuGet Packages**. This downloads all dependencies (including the Google APIs) into the `packages/` directory.
   - If you prefer the command line, run `nuget restore DesktopHelper.sln` (or `dotnet restore` if you have the .NET SDK installed) from the project root to perform the same step before opening the solution.
   - Build the `DesktopHelper` project. Any build errors here must be resolved before you can test the import button.

3. **Launch the application**
   - Run the WPF application from Visual Studio (`F5`) or by launching the compiled executable.
   - Confirm that the main window shows the **Import Next Month** button and the **Create Google Account** link.

4. **Test the Google sign-in flow**
   - Click **Import Next Month**. A browser window should open prompting you to sign in to your Google account and grant read-only access to your calendar.
   - If you do not yet have an account, use the **Create Google Account** action in the app to open the official sign-up page, then restart the import.
   - After completing the OAuth flow, return to the app. The status text should change to reflect the result.

5. **Validate imported tasks**
   - Populate your Google Calendar with events scheduled between the first day of next month and the first day of the following month.
   - Run the import again. Tasks representing the new events should appear in the task list with their due dates and reminder flags.
   - Re-running the import without changing your calendar should not duplicate tasks because the app stores each event's Google `Id` in the `ExternalId` field.

6. **Repeatability tips**
   - Delete the `GoogleCalendarTokens` folder before the next run if you need to test the OAuth consent screen from scratch.
   - Remove or adjust the Google Calendar events to confirm that only items within the next-month window are imported.

## Allowing access to an unverified Google app

If you see an **Access blocked** dialog (often paired with **Error 403: access_denied**)
that says the app has not completed the Google verification process, the OAuth client is
still in **Testing** mode. While in that state Google only allows accounts listed as test
users to sign in. To grant yourself access:

1. Visit the [Google Cloud Console OAuth consent screen](https://console.cloud.google.com/apis/credentials/consent).
2. Ensure the publishing status is **Testing** (you do not need to publish the app for
   personal use).
3. In the **Test users** section, click **Add users** and enter the email addresses that
   should be allowed to sign in (for example, the Gmail account you are using with the
   Desktop Helper app).
4. Save your changes and wait a minute for them to propagate.
5. Restart the import flow in the Desktop Helper app and sign in with one of the test
   accounts you added. Google now allows the authorization to proceed and the app confirms
   the import status once the calendar events load.

If you intend to distribute the application to users outside of the test list, you must
complete Google’s verification process and publish the OAuth consent screen. Until then,
keep the client in testing mode and manage the list of approved test accounts.
