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
![image](https://github.com/user-attachments/assets/b64b3f9c-cca6-41e3-8105-46235420c03a)
6. Press "Select device", choose your pulseoximeter and wait\
![image](https://github.com/user-attachments/assets/cf787717-7837-4f8c-b624-cd1e9e6170dd)
7. Wait until it says "Subscribed to HR notifications" and shows your BPM\
![image](https://github.com/user-attachments/assets/ab695ab5-a95f-426d-b654-462d028a5fbb)

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
