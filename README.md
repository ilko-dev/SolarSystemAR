# SolarSystemAR

An Android AR app (Unity 6) that places the solar system in your room. A cosmonaut guide shows the planets and talks about them, and you can control it by voice in Ukrainian.

## How to use voice

Tap **Говорити** (bottom-right) and say a command. The guide turns cyan while listening.

- **On a phone:** speak. The first time, allow the microphone. Needs internet.
- **In the Unity Editor:** a text box opens instead. Type the phrase and press Enter. The answer appears in the Console.

You don't need exact phrases. Word endings don't matter, and extra words are fine: «покажи, будь ласка, Марс» works.

## What you can say

| To… | Say |
|---|---|
| Find out what the guide can do | «що ти вмієш», «допомога» |
| Show a planet | «Марс», «покажи Юпітер» |
| Hear a fact | «цікавий факт», «розкажи про Сатурн» |
| Hear the discovery history | «історія», «хто відкрив» |
| See which questions you can ask | «які питання» |
| Ask a question | «чому Марс червоний?», «скільки супутників?», «чи можна там жити?» |
| Start the tour | «почни екскурсію», then «далі» / «назад» |
| Pause or resume the planets | «пауза» / «продовж» |
| See the whole system | «огляд» |
| Take a closer look, or leave it | «детальніше» / «вийти» |
| Make the guide stop or repeat | «стоп» / «повтори» |

## If the phone has no Ukrainian voice

Go to **Settings → Text-to-speech**, choose **Google**, and download **Ukrainian**.

## For developers

- Voice phrases are in `Assets/Scripts/Voice/VoiceKeywords.cs`.
- Planet texts and questions are in `Assets/Data/Planets/` (edit them in the Inspector).
- To run the voice tests, which don't need Unity: `dotnet test Tests/VoiceParser.Tests`
