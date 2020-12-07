# Introduction 
Project Coimbra 

# Setup for development
1. Launch solution ProjectCoimbra.UWP/Project.Coimbra.sln
2. Create your own certificate for development by  
    1. In Visual Studio, Solution Explorer, open Project.Coimbra (Universal Windows) / Package.appxmanifest   
    2. Go to **Packaging** tab, click **Choose Certificate...**, then click **Create...** to create your own certificate for development  
3. Replace MIDI reference by  
    1. In Visual Studio, Solution Explorer, open Project.Coimbra (Universal Windows) / References, right click to remove current Microsoft General MIDI DLS for Universal Windows Apps reference (showing exclamation)    
    2. Right click on References > Add Reference..., in the left pane, select Universal Windows > Extensions, and enable **Microsoft General MIDI DLS for Universal Windows Apps**

# Usage
The application has three game modes:
1.	Solo
2.	Offline Band
3.	Online Band

# 1. Solo
In solo mod, you choose a midi song file and the instrument to play. 
The midi songs you have under your Music Library folder will be listed and you can choose one of them or select some other file from another directory. 
After that you will see a list of instruments the chosen song contains. You need to choose an instrument to start playing.

# 2. Offline Band
Offline Band mode lets you to play with other people by starting the music at the same time. You choose a song and an instrument just like you would in the solo mod.
After that you can choose a start time. When other people around you choose the same time to start, you can play the same song together, with different instruments.

# 3. Online Band
To use the Online Band mode, you need to let the app know the IP addresses of all other players. 
To do that you need to create a text file, named "ipAddressesToConnect.txt" in your Music Library folder, and put the line-separated IP addresses inside it. 

When you play the Online Band mode, your application will connect to other applications in over the network and you will be able to play together. 
To use this mode, you need to enter a username, start the game and select an instrument. 
The selected song file will be shared with other players. 
The player who choose the song will act as a conductor and will play the selected instrument and all the other instruments that are not selected by any players. 
The other players will only hear their chosen instrument on their machine.
