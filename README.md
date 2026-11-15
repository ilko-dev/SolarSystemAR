# SolarSystemAR

An Android AR app (Unity 6, AR Foundation + ARCore) that places the solar system in your room, with a cosmonaut guide who shows planets and talks about them. You can control it by voice in Ukrainian.

## Voice control

Tap **Говорити** (bottom-right corner) and say a command. The status line above the button shows what the app heard and whether it understood. The guide turns **cyan** while listening and **green** while talking.

- **On an Android phone:** the phone's built-in Google speech recognition listens, and the Google text-to-speech voice reads the answers aloud. The first tap asks for microphone permission. Recognition usually needs internet.
- **In the Unity Editor / on PC:** tapping **Говорити** opens a text box instead. Type the phrase and press Enter. Spoken answers appear in the Console as `[Voice] Speaking: …`.

You don't have to say a phrase exactly. The app looks for **key words** anywhere in the sentence, and word endings don't matter ("Марс", "Марса", "Марсі" all work). So "покажи, будь ласка, Юпітер" works the same as "Юпітер".

### Commands

| What you want | Say, for example | What happens |
|---|---|---|
| **Show a planet** | «Юпітер», «покажи Марс», «перейди до Венери» | Selects the planet. The guide flies to it and points at it. Any running tour stops. |
| **Interesting fact** | «цікавий факт», «розкажи про Марс», «розкажи» | The guide reads the planet's interesting fact. Uses the planet you named, otherwise the selected one. |
| **Discovery history** | «історія», «хто відкрив», «розкажи історію Юпітера» | The guide reads how the planet was discovered. Uses the named or selected planet. |
| **Ask a prepared question** | «чому Марс червоний?», «скільки супутників у Юпітера?» | If the phrase matches one of the planet's prepared questions, the guide reads its answer (see [Prepared questions](#prepared-questions)). |
| **Start the tour** | «почни екскурсію», «тур», «подорож» | Starts the guided tour: the guide goes through the planets one by one and talks about each. |
| **Next planet** | «далі», «наступна планета», «вперед» | Moves the tour to the next planet. |
| **Previous planet** | «назад», «попередня» | Moves the tour back one planet. |
| **Pause orbits** | «пауза», «зупини», «заморозь» | Planets stop moving around the Sun. |
| **Resume orbits** | «продовж», «запусти», «віднови», «поїхали» | Planets start moving again. |
| **Whole-system view** | «огляд», «покажи всю систему», «всі планети», «сонячна система» | Deselects the planet, stops the tour and narration, and the guide goes back to its home spot. |
| **Closer look** | «детальніше», «ближче», «наблизь», «збільш» | Switches to detail view for the selected planet. If you name a planet («детальніше про Сатурн»), it is selected first. |
| **Leave the closer look** | «вийти», «вихід», «повернись» | Leaves detail view and returns to the selected planet (or to the whole-system view if no planet is selected). |
| **Stop talking** | «стоп», «тихо», «замовкни», «досить», «припини» | The guide stops talking immediately. |
| **Repeat** | «повтори», «ще раз» | The guide says the last thing again. |
| **Cancel** | «скасувати», «відміна» | Same as the whole-system view: resets to the starting state. |

Planet names the app knows: **Меркурій, Венера, Земля, Марс, Юпітер, Сатурн, Уран, Нептун**.

If nothing matches, the guide says «Не зрозумів. Повторіть, будь ласка.»

### When a phrase matches several commands

The app checks in this order and uses the **first** match:

1. **Stop / cancel** words. These always win, even mid-sentence: «стоп, покажи Марс» → stop talking.
2. **Prepared questions.**
3. **Command words**, in the table's internal order. For example, «покажи всю систему» → whole-system view (not "show a planet"), «повернись назад» → leave closer look (not "previous planet"), «розкажи історію» → history (not fact).
4. **Just a planet name** → show that planet.

### Prepared questions

Each planet's data asset (`Assets/Data/Planets/*Data.asset`) has a **Questions** list. Every question has an **id**, the **question** text and the **answer**. The voice system matches what you said against the question text:

- Most of the question's meaningful words must be heard. «чому Марс червоний» matches "Чому Марс червоний?", but «червоний Марс» alone just shows Mars.
- If you name a planet, only that planet's questions are checked. If you don't, all planets are checked, and the selected planet wins a tie («скільки супутників» while Jupiter is selected → Jupiter's question).

To add a question, fill in a new entry in the planet's asset in the Inspector. Voice picks it up automatically, so no code changes are needed.

## For developers

| File | What it is |
|---|---|
| `Assets/Scripts/Voice/VoiceKeywords.cs` | **All voice phrases in one place.** Edit this to add or change words. `слово*` means "any word starting with слово". |
| `Assets/Scripts/Voice/VoiceCommandParser.cs` | Turns recognized text into an `AppCommand`. Plain C#, no Unity code. |
| `Assets/Scripts/Voice/VoiceController.cs` | The **Говорити** button and status line. Sends commands to `AppController.Execute` and speaks `AppController.NarrationRequested` texts. Created automatically in the AR scene (`VoiceBootstrap.cs`), so no scene setup is needed. |
| `Assets/Scripts/Voice/AndroidSpeech*.cs` | Phone speech recognition and text-to-speech (Android built-in, uk-UA). |
| `Assets/Plugins/Android/VoiceBridge.androidlib` | Adds the microphone permission to the Android build. |

### Testing the parser

The parser has unit tests that run without Unity (.NET 8 SDK needed):

```bash
dotnet test Tests/VoiceParser.Tests
```

After changing `VoiceKeywords.cs`, run the tests and add a test for the new phrase in `Tests/VoiceParser.Tests/VoiceCommandParserTests.cs`.

### Phone setup for the Ukrainian voice

If the app says there is no Ukrainian voice: open **Settings → Text-to-speech** (Синтез мовлення) on the phone, choose **Google** as the engine, and download **Ukrainian** in its language settings.
