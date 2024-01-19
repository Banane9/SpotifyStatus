# NeosSpotifyStatus
Shows Spotify status inside Resonite using WebSocket. Also allows for controlling playback (Spotify Premium only), either from just the owner or anyone.
Album, Title and Artists are clickable with hyperlinks to open Spotify to the respective pages. Songs can be queued by pasting or dropping their URIs onto the panel.
A User's Audio Stream can be dropped in as well, and will provide localized volume and broadcast / spatialize controls.

## Server Setup
Make sure you have installed the [.NET 5.0 Runtime](https://dotnet.microsoft.com/download)!
1. Get the latest release of the server [here](https://github.com/Banane9/NeosSpotifyStatus/releases) and extract it somewhere.
2. Create a Spotify application on the [Spotify Developer Dashboard](https://developer.spotify.com/dashboard/applications).
3. Go to the settings of your Spotify application and add ``http://localhost:5000/callback`` as a Redirect URI.
4. Put your application's Client ID and Client Secret into the config.ini file of the server program.
5. Run the program.
6. Sign in with your Spotify account in the browser window that opened and grant the application access.

## Resonite Setup
To actually be able to use this, you will need to get the item from my public folder.
1. Copy this link: ``resrec:///U-Banane9/R-88a9ae63-4861-42e5-a378-bed7468e0e50``
2. Paste it into Resonite by pressing Ctrl+V or by opening your Dash menu and pressing the "Paste content from clipboard" button.
3. Grab the item that just spawned, open your context menu and save it. **(Note: You may need to return to the root of your own inventory to save the folder.)**
4. Open your inventory and enter the folder.
5. Spawn the item.
6. While the server program is running, click on the button that says "Connect to WebSocket".
7. Wait for a bit. This shouldn't take too long, though.
8. Drop your Audio Stream panel into the button being displayed to use the integrated local volume and broadcast / spatialize controls.

## Attributions

Using icons from flaticon.com by Freepik, Pixel Perfect and Kirashastry.
Modified by me for use as white on transparent masks and animations.
