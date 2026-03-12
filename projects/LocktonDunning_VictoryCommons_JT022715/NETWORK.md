# Victory Commons Network Configuration (JT022715)

## IP Standards (generate_dhcp.py)
- **172.22.0.x**: Control Infrastructure (CP4N, Switch)
- **172.22.10.x**: General Devices (Cisco Codecs, Displays, Panels)
- **172.22.20.x**: AV Encoders (NVX-E, AirMedia)
- **172.22.30.x**: AV Decoders (NVX-D)
- **172.22.40.x**: Audio Devices (Tesira, Shure, MXW)

## Core Infrastructure
- **Control System (CP4N)**: `172.22.0.1`
- **Netgear Switch**: `172.22.0.10`

## DHCP Reservations (Source: dhcp_reservations_v2.txt)

| Nickname | MAC Address | IP Address | Switch Port | Category |
| :--- | :--- | :--- | :--- | :--- |
| CP4N_Control | `C4:42:68:85:A4:3C` | `172.22.0.1` | 1 | Control |
| LargeTRMic_1 | `00:0E:DD:66:51:EE` | `172.22.40.11` | 10 | Audio |
| LargeTRMic_2 | `00:0E:DD:66:51:EF` | `172.22.40.12` | 10 | Audio |
| E02-LargeTREncoder | `38:AA:09:A2:75:CB` | `172.22.20.11` | 17 | Encoder |
| E04-SmallTRAirMedia | `C4:42:68:85:2A:B3` | `172.22.20.12` | 19 | Encoder |
| SmallTRDSP_1 | `78:45:01:47:A1:FB` | `172.22.40.13` | 2 | Audio |
| SmallTRDSP_2 | `78:45:01:47:A2:01` | `172.22.40.14` | 2 | Audio |
| E06-LargeTRAirMedia | `C4:42:68:85:28:2F` | `172.22.20.13` | 21 | Encoder |
| D01-SmallTRRoomkit | `C4:42:68:85:C1:5F` | `172.22.30.11` | 22 | Decoder |
| D02-LargeTRRoomkit | `C4:42:68:85:C1:68` | `172.22.30.12` | 23 | Decoder |
| D04-LargeTRDecoder | `C4:42:68:85:C1:33` | `172.22.30.13` | 25 | Decoder |
| D05-LargeTRDecoder | `C4:42:68:83:9F:DF` | `172.22.30.14` | 26 | Decoder |
| LargeTRDSP | `78:45:01:47:A2:3C` | `172.22.40.15` | 3 | Audio |
| E07-HospitalityAirMedia | `C4:42:68:85:C1:6B` | `172.22.20.14` | 36 | Encoder |
| E08-HospitalityEncoder | `C4:42:68:85:C1:73` | `172.22.20.15` | 37 | Encoder |
| D06-HospitalityDecoder | `C4:42:68:85:A2:64` | `172.22.30.15` | 38 | Decoder |

## Static / Manual Assignments (Assumed)
- **Large Planar Left**: `172.22.10.71`
- **Large Planar Right**: `172.22.10.72`
- **Small Planar Display**: `172.22.10.73`
- **Cisco Codec (Large)**: `172.22.10.50`
- **Cisco Codec (Small)**: `172.22.10.51`
- **Hospitality TSW-1070**: `172.22.10.32`

> [!NOTE]
> All DHCP reservations are managed by the CP4N at `172.22.0.1`.
