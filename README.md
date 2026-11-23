<div align="center">

# 🛡️ CYBER THREAT AR

**Learn about cybersecurity threats with Augmented Reality**

![Unity](https://img.shields.io/badge/Unity-2022.3.62f3-20232A?style=flat-square&logo=unity)
![AR Foundation](https://img.shields.io/badge/AR%20Foundation-5.2.2-2563EB?style=flat-square)
![ARCore](https://img.shields.io/badge/ARCore-5.2.2-4285F4?style=flat-square)
![Android](https://img.shields.io/badge/Platform-Android-3DDC84?style=flat-square&logo=android&logoColor=white)

</div>

## About

CYBER THREAT AR is an Android AR app that teaches three common cyber threats: **phishing, malware, and ransomware**. Point the camera at a threat card and an animated 3D model of that threat appears on it. From there you can read about the threat and take a short quiz.

This is an academic project for the Augmented Reality / Virtual Reality course.

## How to Use

1. **Scan** one of the threat cards with your phone camera.
2. **See** the 3D model appear on the card.
3. **Interact**: tap to select, pinch to resize, twist with two fingers to rotate.
4. **Learn**: tapping the model opens the definition, a real-world example, and prevention tips.
5. **Test yourself** with **Uji Pemahaman**, a 3-question quiz on that threat.
6. **Reset / Pindai Ulang** to start over or scan another card.

## Threat Cards

| Phishing | Malware | Ransomware |
|:---:|:---:|:---:|
| <img src="Assets/08_ImageTargets/Phishing_Card_NoBorder.png" width="160" /> | <img src="Assets/08_ImageTargets/Malware_Card_NoBorder.png" width="160" /> | <img src="Assets/08_ImageTargets/Ransomware_Card_NoBorder.png" width="160" /> |
| Fake messages that trick people into giving away personal data. | Malicious software that damages devices or steals data. | Malware that locks your files and demands payment. |
| 3D: envelope with a fishing hook | 3D: bug orbiting a computer | 3D: safe with a moving padlock |

## Features

- **Image tracking** for three cards, each showing its own animated 3D model
- **Touch interaction**: tap highlight, pinch scaling (0.6x–1.8x), two-finger rotation
- **Education panel** with a definition, example, and prevention tips for each threat
- **Contextual quiz** with three A–D questions per threat, instant right/wrong feedback, explanations, and a final score
- **Reset / rescan** that returns the app to its starting state

## Tech Stack

| | |
|---|---|
| Engine | Unity 2022.3.62f3 LTS (URP) |
| AR | AR Foundation 5.2.2 + Google ARCore 5.2.2 |
| Language | C# |
| Target | Android 7.0+ (API 24), ARM64, IL2CPP |

## Getting Started

**Run the APK:** install `CyberThreatAR_Development.apk` on an ARCore-supported Android phone, then scan the cards in `Assets/08_ImageTargets/` (printed or shown on a screen).

**Open in Unity:**

1. Clone this repo and open it with Unity **2022.3.62f3**.
2. Open the scene `Assets/01_Scenes/CyberThreatAR_Main.unity`.
3. Switch the platform to Android (**File → Build Settings**), then **Build** or **Build And Run**.

## Project Structure

```text
Assets/
├── 01_Scenes/        # Main scene
├── 02_Scripts/       # C# scripts (tracking, interaction, education, quiz)
├── 03_Prefabs/       # 3D threat prefabs
├── 04_Models/ 05_Animations/ 06_Materials/ 07_Textures/
└── 08_ImageTargets/  # Threat cards + reference image library
```

| Script | Purpose |
|---|---|
| `ThreatImageTracker` | Detects cards and spawns the matching 3D model |
| `ThreatInteractionManager`, `ThreatInteractable` | Tap, pinch, rotate, and reset |
| `ThreatEducationPanelController`, `ThreatEducationContent` | Threat info panel and its content |
| `ThreatQuizController`, `ThreatQuizContent` | Quiz flow, questions, and scoring |

## Team

| Name | Responsibility |
|---|---|
| Dimas | Project setup, AR foundation (ARCore + image tracking), 3D threat models and animations, final UI redesign and integration |
| Randy | Object interaction (tap, pinch, rotate) |
| Triyanti | Education panel and reset flow |
| Nabil | Contextual quiz |

---

<div align="center"><sub>Third-party assets remain under their respective licenses.</sub></div>
