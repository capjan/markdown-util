# Usage

[Dokumentation](../README.md) • Usage

## Story

- The markdown utility is a command line tool that is easy to set up and is executed in a terminal window
- Open a terminal in the root folder of your documentation to quickly run commands
- For now it's important to be in the root folder because the tool assumes that it is executed in that folder so just stay in that folder

## Running the linter

Start the linter to check your documentation at any time. It's save because it just reads
```bash
mdu lint
```

**Example Output:**

```
Running Linter
Party! 🥳 The Markdown linter does not detect any errors!
```

## Adding breadcrumb navigation

- Generates Breadcrumb Navigation in every markdown files in the current folder and all subfolders
- Must be executed in the root folder of the documentation

```bash
mdu add-breadcrumb-navigation
```

or in short:

```bash
mdu abn
```