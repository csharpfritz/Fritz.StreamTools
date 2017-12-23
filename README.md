# Fritz.Rundown
Handy tools for managing my a live video stream and outputting video widgets that can be used directly in OBS or other streaming tools.

## Features

The project is intended to be built as a Docker container and configured with a series of environment variables.  It is intended to support a single-user, and not run on the public-facing internet.

The following features are supported by this project:

*  A checklist rundown of segments of the show (/index) that is updated from another page (/admin)
*  A followers count API that reports the total number of followers: GET /api/Followers
*  A followers count page that can be easily styled and formatted at /Followers/Count
*  A followers goal meter that can be sized and have its goal caption and value set: /Followers/Goal/{goalValue}/{goalCaption}?width={meterWidthInPixels}

## Services Supported

The project supports reading stream metrics from the following services:

*  Mixer
*  Twitch
