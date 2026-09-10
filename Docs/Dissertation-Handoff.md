# Whitechapel Echoes Dissertation Handoff

This file is for continuing the dissertation-writing work in a new Codex conversation without losing context.

## User Preferences

- The user usually discusses planning, checking and revisions in Chinese.
- The final dissertation text should be written in English.
- When drafting a new section, first explain or negotiate the logic in Chinese if needed, then write polished English into the Word document when the user confirms.
- The user dislikes text that exposes process/background details such as "this was an internship project but the supervisor wanted research content" inside the dissertation itself. Keep those as hidden planning context, not thesis prose.
- The user wants writing to be specific to the project, detailed, and professional, but not stiff or AI-like.
- Use `codex-academic-humanizer` whenever polishing thesis prose. Preserve facts, citations, hedging and argument structure; reduce formulaic AI phrasing.
- Avoid making every subsection the same length. Write with natural weighting: compress simple flow description, expand design reasoning, implementation choices, evaluation interpretation and reflection where useful.
- References should use Harvard style in text and in the reference list.
- Do not overstate historical certainty. Preserve wording such as "may", "could", "possibly" when sources are interpretive.

## Important File Locations

- Main Word document:
  - `C:\Users\KK\Desktop\Whitechapel_Echoes_Dissertation_Opening.docx`
- Project workspace:
  - `D:\Users\KK\Documents\GitHub\Virtual-Archaeology`
- Project brief:
  - `D:\Users\KK\Documents\GitHub\Virtual-Archaeology\Docs\Project-Brief.md`
- This handoff:
  - `D:\Users\KK\Documents\GitHub\Virtual-Archaeology\Docs\Dissertation-Handoff.md`
- User-provided project narration/text document:
  - `D:\Users\KK\xwechat_files\wxid_o71loyofudud22_6135\msg\file\2026-08\Archaeological VR Text(1).docx`
- User-provided reference dissertations:
  - `D:\Users\KK\Desktop\硕士\Semester 3\论文参考\重要\Chi.pdf`
  - `D:\Users\KK\Desktop\硕士\Semester 3\论文参考\重要\Ju Zhang MSc_Goldsmiths Final Report_v5.pdf`
  - `D:\Users\KK\Desktop\硕士\Semester 3\论文参考\重要\Thomas.pdf`
- Questionnaire/results PDF:
  - `D:\Users\KK\Desktop\未命名的表单 - Google 表单.pdf`

Desktop and WeChat paths may require elevated filesystem permission to read/write.

## Current Word Document State

The document `Whitechapel_Echoes_Dissertation_Opening.docx` already contains:

- Title Page
- Abstract
- Acknowledgements
- Contents
- List of Figures
- List of Tables
- Chapter 1: Introduction, including sections 1.1 to 1.3
- Chapter 2: Background and Context
- Chapter 3: Design Rationale
- Chapter 4: Application Design and Implementation
- References

Chapters 3 and 4 were recently compressed and written in English directly into the same document. A final check confirmed the headings appear in this order:

- 3. Design Rationale
- 3.1 Internship Brief and Personal Role
- 3.2 User Journey
- 3.3 Historical and Interaction Design Principles
- 4. Application Design and Implementation
- 4.1 Museum-to-Tavern Experience Flow
- 4.2 Artefact Interaction
- 4.3 Horn Restoration and Performance Interaction
- 4.4 UI Text Panels and User Guidance
- 4.5 Scene, Material and Spatial Design
- 4.6 Audio Integration
- References

The latest revision fixed an accidental reversed insertion order and checked that Chapter 3/4 had no bad question-mark mojibake.

## Dissertation Direction

Working title:

`Whitechapel Echoes: Object-Led and Sound-Led Interaction in a VR Archaeology Experience`

Project name:

`Whitechapel Echoes`

Approximate target length:

- Around 30 pages total.
- Structure is inspired by the three school reference reports, but not copied exactly because the examples are not fully consistent.

The dissertation sits between practice-based/internship reporting and research-oriented evaluation:

- It should mainly present the completed project: design aims, production process, interaction implementation, user testing and reflection.
- It should include enough research/evaluation content to satisfy the supervisor's research direction.
- Do not say this awkwardly in the dissertation. Frame it professionally as a practice-based VR heritage project with user evaluation.

Suggested full structure:

1. Introduction
2. Background and Context
3. Design Rationale
4. Application Design and Implementation
5. User Evaluation
6. Results and Discussion
7. Internship Reflection
8. Conclusion and Future Work

## User's Contribution

User's main contributions:

- Scene interaction
- Artefact interaction
- UI text panels
- Questionnaire design
- User testing
- Material work
- Modelling adjustments
- Unity implementation
- Most scene layout / scene building

Team caveat:

- Audio/music system and audio materials were mainly produced by the teammate.
- The dissertation should not claim the user authored the whole audio system.
- Professional phrasing already used in Chapter 3:
  - "Because the audio and music materials were produced within the team workflow, this report discusses sound primarily as an integrated part of the interaction design, narrative timing and environmental atmosphere, rather than as an individual sound-production portfolio."

## Project Experience Flow

Key user journey:

1. Player begins in darkness.
2. Martha Dawson's voiceover and subtitles gradually introduce the premise.
3. Light reveals a museum/exhibition space.
4. A French horn mouthpiece fragment sits on a white pedestal.
5. Player picks up the fragment.
6. The pedestal focus fades/dims and an incomplete French horn nearby is illuminated.
7. Player is guided to return the mouthpiece to the horn.
8. Horn restoration triggers visual/audio feedback.
9. Museum dissolves and tavern appears.
10. Player enters the tavern and is invited to play the horn.
11. Current horn interaction uses vertical angle alignment.
12. Earlier snap and shake mechanics were removed because playtesting showed they were difficult to understand.
13. After the first melody, three artefacts appear:
    - Bartmann bottle
    - Domino
    - Punch whistle / small figure
14. Touching these objects adds related sound layers to the repeated melody:
    - Bottle: drinking / bottle clinking
    - Domino: tabletop play / gambling ambience
    - Whistle: human / entertainment / whistle-related sound
15. Artefacts unlock for closer inspection.
16. Voiceover and UI panels explain object context.
17. Martha says her wish is fulfilled and the experience ends.

For thesis prose, do not write this as a long walkthrough unless the user asks. Condense it into design logic.

## Narrative Text / Characters

From `Archaeological VR Text(1).docx`:

- Main guide voice: Martha Dawson.
- Martha is presented as the landlady of the tavern and guardian of its echoes.
- Unnamed musician:
  - travelling horn player
  - remembered less by name than by horn sound
  - appears as a faint ghostly memory in the garden

Useful narration concepts:

- "clinking of bottles, laughter, rhythm of life"
- tavern sounds faded long ago
- player came for the horn
- only a fragment remains
- return it to where it belongs
- old ghosts / echoes of the tavern waking up
- the horn was at the heart of the tavern's sound
- Tower Hamlets March is used in the script, but verify before making strong historical claims.

Do not claim Martha Dawson is historically verified unless a source is provided or checked. Safer phrasing:

- "the narrative guide Martha Dawson"
- "the landlady figure Martha Dawson"

## Sound Design Notes

Opening/museum:

- Quiet and restrained.
- Purpose:
  - helps the user focus on Martha's voice and subtitles
  - creates contrast with the later tavern
  - avoids overwhelming the first task

Tavern:

- Richer and more layered.
- Area-based sounds:
  - road / carriage / exterior sounds near street
  - fire crackling near hearth
  - birds near outdoor/tree areas
  - interior tavern ambience
  - window/outside ambience
  - voices, bottles, music

Interpretive point:

- There are no visible living people in the tavern.
- Sound makes the space feel socially inhabited without staging a literal reenactment.
- The past is represented as an echo, memory or projection rather than a fully recovered reality.

Where to place audio critique:

- In Chapter 4, describe integration and design purpose.
- In Chapters 5/6, discuss user feedback, clarity issues and possible improvements.

## Gaussian Splatting / PLY Notes

Museum:

- Uses more conventional assets/FBX-style stable display language.
- Represents present-day museum/exhibition frame.

Tavern / past:

- Uses PLY scene data and Gaussian splatting effects.
- Reason:
  - FBX felt too solid, ordinary and certain.
  - Gaussian splatting/PLY gives a more fragmentary, dreamlike, unstable feel.
  - Combined with wave-like or flowing light, it signals that the tavern is a mediated past, not a complete return to reality.

Academic framing:

- Cite Kerbl et al. (2023) for 3D Gaussian Splatting.
- Avoid making the section too technical. Explain only what supports the design argument.

## Research / Citation Sources Already Used

Use Harvard style.

Key sources already in the document/reference list:

- Archaeology South-East (n.d.) Whitechapel. UCL Institute of Archaeology.
- UCL Social & Historical Sciences (n.d.) Whitechapel. University College London.
- Bekele et al. (2018), VR/AR/MR for cultural heritage.
- Chong et al. (2021), systematic review of VR for cultural heritage.
- Puig et al. (2019), lessons from archaeological museum VR.
- Slater (2009), place illusion and plausibility.
- Zhang and Chen (2024), VR museum interpretation and perceived authenticity.
- Privitera, Fontana and Geronazzo (2024), audio in immersive cultural heritage storytelling.
- Kaplan-Rakowski, Cockerham and Ferdig (2023), sound and immersive VR learning.
- Kerbl et al. (2023), 3D Gaussian Splatting.

Primary historical facts from UCL/ASE sources:

- Whitechapel excavations by Archaeology South-East, 2015-2019.
- Approx. 7,300 square metres excavated.
- Site includes multi-period evidence: Iron Age, medieval manor, Red Lion Playhouse, Georgian taverns including the White Raven, Victorian housing and later industrial use.
- French horn mouthpiece:
  - copper alloy
  - second half of the eighteenth century
  - linked interpretively to tavern/music context
- Anneke Scott performed Tower Hamlets March in the UCL material.
- Phrases such as "might have been heard" should stay uncertain.
- White Raven has difficult Black history connections involving the Committee for the Relief of the Black Poor and coerced journeys to Sierra Leone. Treat this carefully, without dramatizing unsupported details.

## Existing Useful Code / Project Files

Useful scripts if technical implementation needs checking:

- `Assets/Scripts/MuseumExperienceController.cs`
  - main state machine and museum-to-tavern flow
  - states include MuseumIntro, WaitForPiecePickup, WaitForPieceInsertion, HornRestoration, EnvironmentTransition, FreeExploration
- `Assets/Scripts/HornFMODController.cs`
  - horn performance interaction
  - code may still show older mode names; user says final version removed snap/shake and uses angle-based movement
- `Assets/Scripts/NarrationManager.cs`
  - voiceover/subtitles
- `Assets/Scripts/ArtifactInfoPanel.cs`
  - UI text panel display
- `Assets/Scripts/ArtifactInfoOnGrab.cs`
  - artefact info on grab
- `Assets/Scripts/TapToggleGlow.cs`
  - glow, haptic/audio feedback, object-triggered layers
- `Assets/Scripts/SingleSceneAudioEnvironmentManager.cs`
  - museum/tavern audio transition and 3D audio zones
- `Assets/Scripts/GsplatSegmentRevealController.cs`
  - Gaussian splat reveal/visual effects

If writing about implementation, read the relevant files first instead of relying only on memory.

## Current Writing Decisions

Decisions already made:

- Keep Research Questions to a small number; earlier draft used three, but user questioned whether three are necessary. Revisit if needed.
- Do not state questionnaire weakness as "small sample" in a self-undermining way. Discuss percentages and patterns, and frame the method as appropriate to project evaluation.
- User evaluation should not be inflated into a large scientific experiment.
- Playtest-driven removal of snap/shake belongs in implementation/design iteration, with fuller discussion in evaluation/results.
- Long user-flow narration should be compressed.
- Audio production should be credited carefully as team work; audio integration can be discussed as part of the user's implementation/design work.

## How To Continue

Likely next task:

- Continue writing Chapter 5: User Evaluation, using the questionnaire PDF and possibly user testing notes.

Before writing Chapter 5, ask or verify:

- Number of participants.
- Whether participants were classmates, general users, VR-experienced users, or mixed.
- Whether testing happened in person, with headset, or through a demo video/build.
- Key questionnaire questions and response patterns.
- Any open-ended comments.
- Whether demographic details should be included or kept minimal.

Suggested Chapter 5 structure:

- 5. User Evaluation
- 5.1 Evaluation Aim
- 5.2 Participants and Procedure
- 5.3 Questionnaire Design
- 5.4 Data Analysis Approach
- 5.5 Ethical and Practical Considerations

Suggested Chapter 6 structure:

- 6. Results and Discussion
- 6.1 Understanding of Artefacts and Historical Context
- 6.2 Immersion, Sound and Atmosphere
- 6.3 Interaction Clarity and Usability
- 6.4 Limitations
- 6.5 Design Improvements

Keep evaluation prose confident but not overclaiming.
