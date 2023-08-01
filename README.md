# SCModRepository

<p align="center">
    <img src="https://github.com/StarCoreSE/SCModRepository/assets/51190031/c413613b-08e1-48de-a763-2adfe7fa871f" width="480">
</p>


**Starcore** is a community built around modding space engineers, usually for PVP scenarios.

In StarCore, teams build their own ships and battle for the spot of champion in a StarCore Tournament. They normally take place on Saturdays and are streamed live by one of several streamers over on Twitch (See #content-announcements in the StarCore Discord for more information).  It's time to join the arena!


## Contribution guide

### prerequsites:
- (extremely recommended) github Desktop, GitExtentions, or something similar
- knowing the layout of SE mod files
- enough space to download the entire git repo (~5gb)?

### Step 1:
- ``Fork`` this repository to a folder on your computer. Name it something like SCModRepository-Yourname. This is where your edits can be made, and is apparently how actual projects do it.

### Step 2:
- ``Make a branch`` for the changes you want to do on ``your local repository``. (i.e. SCModRepository-Yourname/BuffMyFavoriteGunPlease) Use your local repository's ``Main`` branch to keep in sync with starcore's ``Main`` branch, it makes edits much easier. You just click the button on github to sync it.

### Step 3:
- To test your changes ingame, Copy the mod you want to edit to your ``%Appdata%/SpaceEngineers/Mods`` folder.
 
### Step 4:
- Make your edits and throw it back in the repository folder. you can use the ``.bat file`` included in the repository to link your local Space Engineers mods with the ones in the repository.

### Step 5:
- Submit a pull request so that the branch can be merged into the SCModRepository/master one.






## How does this work?
The repository contains a .github folder, Space Engineers mod folders, and a .gitignore file.
### .github folder:
- contains the instructions to the bot what to do after a "push", currently set to upload to the steam workshop after the respective .yml file detects a change in the folder its looking for
### SE Mod folders:
- contains all the data that would load normally as an SE mod
### .gitignore file
- tells git what to exclude during a push (like .sln files in visual studio)
