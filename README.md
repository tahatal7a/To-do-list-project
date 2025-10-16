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
