# LibreUTAU

LibreUTAU aims to be an open source editing environment for UTAU community,
with modern user experience and intelligent phonological support.

Forked from [OpenUtau](https://github.com/stakira/OpenUtau).

Current status: Highly unstable

## Current functionality

* Creating vocal parts (Works with some voicebanks/resamplers)
* Importing audio files
* Basic sequencing functions
* Feature-rich MIDI editor
* Loading UST/VSQx projects

## Development plan
#### Known bugs and issues
* Sampling issues
    * Vocal part is not played if it has a note in the very beginning of the project timeline
    * Sometimes sound is not played at all
    * Sound glitches if there is more than one voice part in the project
* Wacky playback controls
    * Project beat properties and BPM are not editable
    * Timeline not updated when Go To Beginning/Go To End buttons are pressed
    * Different buttons for Play and Pause
* Misc UI glitches
    * Keypresses are interpreted as commands when note lyric edited
    * Expression controls are not properly updated sometimes
    
#### Features yet to implement (sometime in the future)
* Automatic updater
* Convenient singer import interface
* Singer editor
* Numerous UI tweaks
   