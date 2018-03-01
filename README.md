# Fritz.StreamTools
Handy tools for managing my a live video stream and outputting video widgets that can be used directly in OBS or other streaming tools.

## Features

The project is intended to be built as a Docker container and configured with a series of environment variables.  It is intended to support a single-user, and not run on the public-facing internet.

The following features are supported by this project:

*  A checklist rundown of segments of the show (/rundown) that is updated from another page (/admin)
*  A followers count API that reports the total number of followers: GET /api/Followers
*  A followers count page that can be easily styled and formatted at /Followers/Count
*  A followers goal meter that can be sized and have its goal caption and value set, complete with configuration screen at /Followers/Goal/Configuration
![Follower Goal Sample](docs/images/FollowerGoalSample.PNG)

## Services Supported

The project supports reading stream metrics from the following services:

*  Mixer
*  Twitch

## Contributing

This application was built with ASP.NET Core 2.0 and can be built on Mac, Linux, and Windows.  Download the [.NET SDK](https://dot.net) and grab a copy of [Visual Studio Code](https://code.visualstudio.com) to get started on any platform

### Naming guideline for unit tests:
*  Create a folder for each "logical class"
*  Create a test class for each feature to test - end with "Should"
*  Test Methods should describe what they are inspecting and what they're given, if anything.. ending with "\_Given..."
Example: StreamService.CurrentFollowerCountShould.MatchCurrentFollowerCount_GivenOneService
