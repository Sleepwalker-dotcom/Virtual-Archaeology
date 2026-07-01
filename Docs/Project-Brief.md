# Whitechapel VR Archaeology / UCL-ASE Pilot

Read this brief before making Unity changes. It provides the minimum project
context needed to understand the prototype and respond correctly to development
tasks.

## Project overview

This is a Unity VR heritage and archaeology prototype based on the Whitechapel
excavations by Archaeology South-East, UCL Institute of Archaeology. The pilot
uses layered storytelling to connect archaeological objects, sound and music,
and reconstructed historical environments.

The main user experience is:

1. The user begins in a present-day or museum-like display context.
2. The user sees an archaeological fragment, currently focused on a French horn
   mouthpiece or music-related object.
3. The user picks up the fragment in VR.
4. The fragment can become visually complete or act as the trigger for
   reconstruction.
5. When the user brings the object near the mouth or headset area and performs a
   play-like gesture, sound begins.
6. The surrounding historical scene gradually appears around the user.
7. The final revealed scene is a late 18th-century tavern or alehouse environment
   connected to the White Raven, White Lion, and Red Lion context in Whitechapel
   and Mile End.
8. Continued movement, tilt, or playing gestures can switch or modulate
   music/audio and reveal more narrative layers.

## Historical and archaeological context

The archaeological site is at Stepney Way / Whitechapel, within Mile End, east
London. Excavations took place from 2015 to 2019 before redevelopment. The site
produced evidence across multiple periods, including Iron Age activity, a
medieval manor, the Red Lion Playhouse, Georgian taverns including the White
Raven, Victorian houses, and later industry.

The prototype currently focuses on the long 18th century, especially
approximately the 1770s to 1786. The most relevant historical setting is the
White Raven / White Lion tavern context around Mile End Green. This was a social,
leisure, drinking, smoking, gaming, and music environment. It is also connected
to the Committee for the Relief of the Black Poor in 1786 and to the history of
Black people in late 18th-century London.

Important narrative anchors:

- Whitechapel / Mile End was a changing edge-of-London environment: partly
  urban, partly open land, with taverns, yards, gardens, fields, stables, and
  leisure spaces.
- The Red Lion estate was associated with the earlier Red Lion Playhouse, one of
  the earliest purpose-built playhouse contexts in Britain.
- The White Raven / White Lion tavern sequence is the main tavern context for
  the prototype.
- In 1786 the White Raven was associated with the Committee for the Relief of
  the Black Poor.
- John Pegg, described in the source material as one of the committee's
  corporals, is connected to a documented incident at the White Raven in
  September 1786.
- This Black history material is important but sensitive. Do not invent
  additional claims or dramatise it beyond the supplied narrative direction.

### UCL-confirmed interpretive anchors

The UCL Whitechapel project page is a primary project source and confirms the
following points:

- Archaeology South-East excavated a copper-alloy French horn mouthpiece dating
  to the second half of the 18th century.
- Historical horn player Anneke Scott performed the *Tower Hamlets March*, a
  piece documented in an 18th-century horn tutor book, in response to the find.
  UCL presents the possibility that it was heard in local taverns as a question,
  not as an established fact. The VR experience must preserve that uncertainty.
- The 17th- and 18th-century inn assemblages may relate to the White Raven
  tavern. This relationship should likewise not be presented as more certain
  than the evidence allows.
- The White Raven is connected to the coerced 1786 resettlement journeys of
  members of London's Black community to West Africa, organised by the
  Committee for the Relief of the Black Poor. UCL states that more than 400
  people sailed for the newly established colony of Sierra Leone and that only
  315 survived the journey.
- UCL's interpretation was developed with community historians and asks who
  interprets archaeological sites and how commercial archaeology can provide
  public benefit. The VR narrative should retain this community-led,
  evidence-aware approach.

## Scene direction

The target environment is a late 18th-century East London tavern or alehouse,
not a modern pub.

Useful scene elements:

- Dark timber beams and worn wooden furniture.
- Brick, plaster, or limewashed walls.
- Wooden tables, benches, barrels, shelves, and storage areas.
- Ceramic drinking vessels, glass bottles, and wine glasses.
- Clay tobacco pipes.
- Gaming and leisure objects such as skittles, dice, dominoes, cards, or
  bowling/skittle-ground references.
- A possible yard, pleasure garden, or skittle-ground transition outside the
  tavern.
- Warm candle or oil-lamp-style lighting, with no modern electric lights or
  modern objects.
- A crowded, layered soundscape: tavern ambience, voices, footsteps, drinking
  vessels, distant street sounds, and music.

Do not treat the scene as a fantasy medieval tavern. It should feel Georgian and
late 18th-century East London: worn, practical, socially busy, and materially
grounded in the archaeological finds.

## Interaction design

The core interaction is object-led and sound-led.

Primary interaction loop:

- The user sees an artefact fragment.
- The user picks up the artefact.
- Holding or inspecting it may show a complete reconstruction of the object.
- Bringing the object near the mouth or headset area triggers playing.
- Playing triggers audio and starts revealing the historical scene.
- The scene reveal should be gradual rather than an instant hard cut where
  possible.
- Further movement, tilt, rotation, or gesture intensity can control audio
  variation or switch musical layers.

The artefact interaction system should be modular. It should not be hardcoded
only for one object unless specifically requested.

## Unity development assumptions

This is a Unity VR project. Current development is focused on getting a playable
prototype working, not building the final polished experience.

Likely development tasks:

- VR grab and pickup interaction.
- Object-near-mouth trigger using headset/camera distance.
- Timed hold trigger, such as holding near the mouth for one to two seconds.
- Event-driven scene reveal.
- Audio playback and audio-layer switching.
- Simple UI or narration prompts.
- Scene transition from display case to tavern reconstruction.
- Basic optimisation for VR.

The project may include FBX models for conventional 3D scenes and PLY assets for
experimental point-cloud or Gaussian-splat-style rendering. Do not assume all
PLY files contain valid Gaussian splat data. Large binary assets should remain
managed through Git LFS.

## Current implementation priority

Focus on a first playable prototype:

1. Stable VR player setup and locomotion.
2. Artefact pickup.
3. Mouth/headset proximity trigger.
4. Audio trigger.
5. Gradual tavern scene reveal.
6. Movement/tilt-based audio variation.
7. Basic contextual UI or narration.
8. Visual polish and historical detail.

## How to respond to development tasks

When asked to implement something:

- Read this brief first.
- Make the smallest useful Unity change for the requested task.
- State which files were changed.
- For each new Unity script, state exactly which GameObject it should be attached
  to and which Inspector fields must be assigned.
- Avoid deleting, renaming, or moving existing assets unless the task explicitly
  requires it.
- If a historical feature is uncertain, label it as speculative rather than
  presenting it as fact.

## Example task interpretation

If asked to create the artefact-playing trigger, implement a reusable script
where:

- A grabbable artefact checks its distance to the VR camera or mouth proxy.
- If the object stays within range for a short duration, it fires a UnityEvent.
- The event can be connected in the Inspector to audio playback, scene reveal,
  or narration.
- The script exposes range, hold time, and debug options in the Inspector.

## Authoritative sources

Use these sources when checking historical claims or planning interpretive
content. A source link is not permission to copy text or media; check licensing
and attribution before reusing images, video, audio, or substantial text.

1. [UCL Whitechapel project and PATTERNS exhibition][ucl-whitechapel] — primary
   source for the public interpretation, French horn mouthpiece, *Tower Hamlets
   March*, White Raven narrative, project team, and bibliography.
2. [Archaeology South-East: Whitechapel archaeological discoveries][ase-whitechapel]
   — primary project overview for the 2015–2019 Stepney Way excavations,
   approximately 7,300 square metres of investigated stratigraphy, and the
   multi-period sequence.
3. Publications listed on the UCL project page should be preferred for detailed
   archaeological claims. Record page numbers when transferring a claim into
   scripts, narration, labels, or visual reconstruction notes.

Web sources can change. Record the access date and supporting quotation or page
reference in production research notes before turning a claim into final
narration.

[ucl-whitechapel]: https://www.ucl.ac.uk/social-historical-sciences/research-projects/whitechapel
[ase-whitechapel]: https://www.ucl.ac.uk/archaeology-south-east/research/projects/whitechapel
