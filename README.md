# HeartRateMonitorVRC
![e1af8ed2bd59eea3e71613963807320a](https://github.com/user-attachments/assets/7edd955b-0d67-40cf-9ad3-17d99960ae6b)\
![6c64df81b1fff43bbe6966cb85eb9ed0](https://github.com/user-attachments/assets/577995e1-a97d-467e-ba65-c2b7d6b1e7f7)\
(Those are two different gifs, that's why they are not synced)

An application for streaming heart rate data from a pulse oximeter directly to VRChat without the need for phone or web apps, using desktop Bluetooth and VRChat OSC. Alternatively, it can use the [helper phone app](https://github.com/DangerKiddy/AndroidHeartRateMonitorVRC) but still without web apps and completely free.

## Requirements
- Pulseoximeter with BluetoothLE (Low energy) support, such as CooSpo H808S (Tested only with that device)
- One of down below:
  - Bluetooth adapter for PC/Built-in Bluetooth in motherboard of PC
  - Or a phone with [this app](https://github.com/DangerKiddy/AndroidHeartRateMonitorVRC) and PC connected to the same internet
  
## Connecting via Bluetooth
1. Open Bluetooth settings\
![image](https://github.com/user-attachments/assets/3b259898-fb43-4bc2-8c2b-a12200d68541)
2. Press "Add Bluetooth or other device"\
![image](https://github.com/user-attachments/assets/c98dfda8-ed66-41c8-9ebc-2fccf2b297db)
3. Select "Bluetooth" and connect your pulseoximeter\
![image](https://github.com/user-attachments/assets/27b27978-246f-4d32-a929-4e5cc1329226)
\
For Windows 11 you must set Advanced bluetooth discovery\
![image](https://github.com/user-attachments/assets/99cc838f-33c1-413e-bdb6-5b6bd0903a02)
\
![image](https://github.com/user-attachments/assets/b8689d3b-2438-49e4-b231-0220a1a053b8)
5. Open HeartRateMonitorVRC\
![image](https://github.com/user-attachments/assets/429bb415-a4cd-47a3-b802-13b7df5e4cb0)

6. Press "Select device", choose your pulseoximeter and wait\
![image](https://github.com/user-attachments/assets/e4c47c26-4a51-465c-b12e-dda8529ad2ce)
7. Wait until it says "Subscribed to HR notifications" and shows your BPM\
![image](https://github.com/user-attachments/assets/8e7f610d-4455-40b9-81c0-a023e755df69)

## Connecting via phone app
1. Connect your PC and phone to the same internet
2. Launch the HeartRateMonitorVRC on your PC
3. Launch HeartRateMonitorVRC on your phone
4. Turn on your pulseoximeter and press "Scan" button in the phone app
5. Wait for 10 seconds until it displays all Bluetooth devices nearby
   - If you cannot see your device in the list press scan again until it shows. If it still not appeared then please refer to the [issues page](https://github.com/DangerKiddy/HeartRateMonitorVRC/issues) and create a new issue
6. Select your pulseoximeter device and wait until your heart rate appears on PC application
7. Done

## Troubleshooting
### Unable to get characteristics / Failed subcribing to HR notifications
Happens if your device is occupied by another device (i.e. connected to another device). If you're reconnecting then simply wait a bit and try again
### Heart rate isn't sending anymore when using phone app
This issue happens when your phone blocks the screen/goes in sleep mode. This would be fixed in the future versions, but for now please find a way to keep your phone awake, such as enabling a 10 hour video

## Integrating with VRChat
HeartRateMonitor sends OSC messages to VRChat port (9000) using parameters from [HRtoVRChat_OSC](https://github.com/200Tigersbloxed/HRtoVRChat_OSC/blob/main/AvatarSetup.md#supported-parameters) and [vrc-osc-miband-hrm](https://github.com/vard88508/vrc-osc-miband-hrm), but also includes some exclusive parameters
### Used parameters
| Address | Type  | Range          | Description                                         | Is Exclusive |
| ------- |-------|----------------|-----------------------------------------------------| ----------- |
| /avatar/parameters/onesHR | int   | `0`-`9`        | Ones spot in the Heart Rate reading                 | &cross; |
| /avatar/parameters/tensHR | int   | `0`-`9`        | Tens spot in the Heart Rate reading                 | &cross; |
| /avatar/parameters/hundredsHR | int   | `0`-`9`        | Hundreds spot in the Heart Rate reading             | &cross; |
| /avatar/parameters/isHRBeat | bool  | `True`/`False` | Estimation on when the heart is beating             | &cross; |
| /avatar/parameters/HRPercent | float | `0`-`1`        | Converted range of HR                               | &cross; |
| /avatar/parameters/FullHRPercent | float | `-1`-`1`       | Other converted range of HR                         | &cross; |
| /avatar/parameters/HR | int   | `0`-`255`      | Exact HR value                                      | &cross; |
| /avatar/parameters/Heartrate2  | float | `0`-`1`        | Converted range of HR                               | &cross; |
|/avatar/parameters/RangePercent| float | `0`-`1`            | Range from lowest to highest calculated heart rate  |&cross;| 
| /avatar/parameters/HeartrateLowest  | float | `0`-`1`        | Converted range of Lowest HR for the whole session  | &check; |
| /avatar/parameters/HeartrateHighest  | float | `0`-`1`        | Converted range of Highest HR for the whole session | &check; |

## Building
To build the project you need to manually include Windows.winmd and System.Runtime.WindowsRuntime.dll in the project, which may be located here:
- C:\Program Files (x86)\Windows Kits\10\UnionMetadata\10.0.22621.0\Windows.winmd (Also shipped in Release)
- C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Runtime.WindowsRuntime.dll
