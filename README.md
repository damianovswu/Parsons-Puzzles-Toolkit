# Repository Status

This repository contains the implementation that accompanied the publication:

> Damyanov, I. & Madzhov, M. (2026) Enhancing Computational Thinking and Code Comprehension Through Advanced Parsons Puzzles, Mathematics and Informatics Journal 69(2), pages 190-206, https://doi.org/10.53656/math2026-2-5-ect

It is preserved to ensure reproducibility of the results reported in the paper and to provide a stable reference for readers.

Research and software development have continued after the publication. The actively maintained version of the project, including new functionality, improvements, and ongoing contributions, is available at:

[https://github.com/idamianov/ParsonsPuzzleApp](https://github.com/idamianov/ParsonsPuzzleApp)

This repository remains available as a historical snapshot of the software used during the preparation of the publication.


---



# Parsons-Puzzles-Toolkit

The **Parsons Puzzle Toolkit**, recently presented at [FMNS-2025 in Blagoevgrad, Bulgaria](https://fmns2025.swu.bg/), represents a significant advancement in educational tools aimed at strengthening computational thinking and code comprehension skills. Building upon the original concept introduced by [Dale Parsons and Patricia Haden in 2006](https://dl.acm.org/doi/10.5555/1151869.1151890), this new iteration addresses the limitations of earlier implementations by introducing a comprehensive and highly adaptable framework designed to support a wider range of programming languages, educational contexts, and cognitive learning strategies.

<img width="949" height="557" alt="image" src="https://github.com/user-attachments/assets/9d2c0dbe-94d7-4d74-8789-2d33c99a3912" />

Parsons Puzzles are structured as code-reordering exercises, where students reconstruct correct programs from shuffled code lines. By removing the burden of syntax, these puzzles allow learners to focus on logical sequencing and algorithmic reasoning. However, traditional Parsons Puzzle tools tend to be confined to one or two languages - often with similar imperative structures - and offer limited flexibility in task design. The **Parsons Puzzle Toolkit** overcomes these constraints by introducing multi-language support (C, C++, C#, Java, JavaScript, Python, and SQL), multi-paradigm compatibility, and advanced features that support more complex learning scenarios.

Key innovations include:

* Flexible support for languages with dissimilar syntax, 
* Multi-line puzzle blocks, 
* An improved version for handling nested constructions, 
* Mini-blocks for constructing more complex puzzles in realistic scenarios,
* Support for “fill-in-the-blank” puzzles.

<img width="925" height="436" alt="image" src="https://github.com/user-attachments/assets/40a200cc-e19a-4d69-a7ee-e96f20b2c12f" />

Beyond its technical features, the toolkit is also pedagogically significant. By blending the visual intuitiveness of block-based learning with the rigor of text-based coding, the toolkit functions as a bridge for students transitioning from environments like Scratch to modern general-purpose languages. This hybrid model supports learners at various levels, from absolute beginners to those making the leap into industry-standard development languages.

The toolkit's visual interface and logical segmentation support through structured, engaging, and interactive problem-solving tasks the core elements of computational thinking:
- **decomposition**,
- **pattern recognition**,
- **abstraction**,
- **algorithmic design**.

This positions the Parsons Puzzle Toolkit not only as an effective classroom tool but also as a foundational component in the broader landscape of computer science education.

By expanding the pedagogical reach and technical capacity of traditional Parsons Puzzles, this toolkit offers a future-ready solution for programming education. Whether in introductory computer science courses, specialized programming tracks, or database instruction, it delivers a highly adaptable and impactful learning experience that aligns with modern educational goals.


## Technologies Used

* .NET 8 
* ASP.NET Core Razor Pages
* Entity Framework Core
* SQLite

## Getting Started

In order to get your SQLite DB created execute the following 

```
dotnet ef database update
```

---

## Sample Parsons Puzzles
- [C++ Standard 2D Puzzle](http://194.141.86.248/bundle/b07884e9-22e0-4004-81a8-05aaa04d83bc) (Key to Unlock: `123`)
- [Python Standard 2D Puzzle](http://194.141.86.248/bundle/75489b30-b603-4560-9606-a094f9347642) (Key to Unlock: `123`)
- [Fill-in-the-blank SQL Puzzle](http://194.141.86.248/bundle/6abf8830-205f-4009-ad01-210012459896) (Key to Unlock: `123`)

