# HeartRateMonitorVRC
## Requirements
- Pulseoximeter with BluetoothLE (Low energy) support, such as CooSpo H808S (Tested only with that device)
- Bluetooth adapter on PC

## Connecting
1. Open Bluetooth settings\
![image](https://github.com/user-attachments/assets/3b259898-fb43-4bc2-8c2b-a12200d68541)
2. Press "Add Bluetooth or other device"\
![image](https://github.com/user-attachments/assets/c98dfda8-ed66-41c8-9ebc-2fccf2b297db)
3. Select "Bluetooth" and connect your pulseoximeter\
![image](https://github.com/user-attachments/assets/27b27978-246f-4d32-a929-4e5cc1329226)
\
![image](https://github.com/user-attachments/assets/b8689d3b-2438-49e4-b231-0220a1a053b8)
5. Open HeartRateMonitorVRC\
![image](https://github.com/user-attachments/assets/429bb415-a4cd-47a3-b802-13b7df5e4cb0)

6. Press "Select device", choose your pulseoximeter and wait\
![image](https://github.com/user-attachments/assets/e4c47c26-4a51-465c-b12e-dda8529ad2ce)
7. Wait until it says "Subscribed to HR notifications" and shows your BPM\
![image](https://github.com/user-attachments/assets/8e7f610d-4455-40b9-81c0-a023e755df69)

## Troubleshooting
### Unable to get characteristics / Failed subcribing to HR notifications
Happens if your device is occupied by another device (i.e. connected to another device). If you're reconnecting then simply wait a bit and try again

## Integrating with VRChat
HeartRateMonitor sends OSC messages to VRChat port (9000) using parameters from [HRtoVRChat_OSC](https://github.com/200Tigersbloxed/HRtoVRChat_OSC/blob/main/AvatarSetup.md#supported-parameters) and [vrc-osc-miband-hrm](https://github.com/vard88508/vrc-osc-miband-hrm)
### Used parameters
| Address | Type | Range | Description |
| ------- | ---- | ----- | ----------- |
| /avatar/parameters/onesHR | int | `0`-`9` | Ones spot in the Heart Rate reading |
| /avatar/parameters/tensHR | int | `0`-`9` | Tens spot in the Heart Rate reading |
| /avatar/parameters/hundredsHR | int | `0`-`9` | Hundreds spot in the Heart Rate reading |
| /avatar/parameters/isHRBeat | bool | `True`/`False` | Estimation on when the heart is beating |
| /avatar/parameters/HRPercent | float | `0`-`1` | Converted range of HR |
| /avatar/parameters/FullHRPercent | float | `-1`-`1` | Other converted range of HR |
| /avatar/parameters/HR | int | `0`-`255` | Exact HR value |
| /avatar/parameters/Heartrate2  | float | `0`-`1` | Converted range of HR |

## Building
To build the project you need to manually include Windows.winmd and System.Runtime.WindowsRuntime.dll in the project, which may be located here:
- C:\Program Files (x86)\Windows Kits\10\UnionMetadata\10.0.22621.0\Windows.winmd (Also shipped in Release)
- C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Runtime.WindowsRuntime.dll
