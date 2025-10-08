# Quick Start Guide - New Features

This guide shows you how to use the new features and improvements.

## 🎨 Feature 1: Markdown Support in AI Feedback

### What is it?
AI feedback now supports rich text formatting using Markdown syntax.

### How to use it?

1. **Enable AI Mode:**
   - Click the ⚙️ Settings icon
   - Toggle "AI Augmented Cards" ON
   - Enter your LLM endpoint, model name, and API key
   - Click "Save Settings"

2. **Get Markdown Feedback:**
   - Review a deck
   - Type your answer
   - Submit the answer
   - AI feedback will be rendered with formatting

### Supported Markdown Elements:

**Bold text:**
```
**This is bold**
```
Result: **This is bold**

**Italic text:**
```
*This is italic*
```
Result: *This is italic*

**Code blocks:**
```
```python
def hello():
    print("Hello, World!")
```
```

**Lists:**
```
- Item 1
- Item 2
- Item 3
```

**Headings:**
```
### This is a heading
```

**Blockquotes:**
```
> This is a quote
```

**Tables:**
```
| Column 1 | Column 2 |
|----------|----------|
| Data 1   | Data 2   |
```

## ⌨️ Feature 2: Enter Key to Submit Answer

### What is it?
You can now submit your answer by pressing the Enter key instead of clicking the button.

### How to use it?

1. **Enable AI Mode** (see Feature 1)

2. **Review a card:**
   - Go to review mode
   - Type your answer in the textarea

3. **Submit:**
   - Press **Enter** → Submits the answer
   - Press **Shift + Enter** → Creates a new line (for multi-line answers)

### Keyboard Shortcuts:

| Key Combination | Action |
|----------------|--------|
| Enter | Submit answer |
| Shift + Enter | New line in answer |

## 👆 Feature 3: Fixed Card Reveal (AI Disabled)

### What is it?
When AI mode is disabled, you can tap/click the question card to reveal the answer.

### How to use it?

1. **Disable AI Mode:**
   - Click ⚙️ Settings
   - Toggle "AI Augmented Cards" OFF
   - Click "Save Settings"

2. **Review cards:**
   - Go to review mode
   - You'll see "Tap to reveal answer" hint
   - Click/tap the question card
   - Answer card will slide in

3. **Grade the card:**
   - Choose Again, Hard, Good, or Easy
   - Next card loads automatically

## 🏗️ For Developers: Code Architecture

### New Project Structure:

```
frontend/src/
├── components/          # UI components
├── hooks/              # Custom React hooks ⭐ NEW
│   ├── useCardReview.ts
│   ├── useAIAnswer.ts
│   └── useLocalStorage.ts
├── constants/          # App constants ⭐ NEW
│   ├── chartColors.ts
│   └── messages.ts
├── utils/              # Utility functions ⭐ NEW
│   └── dateFormatter.ts
├── services/           # External services
├── types.ts
└── api.ts
```

### Using Custom Hooks:

**Example: useCardReview hook**
```typescript
import { useCardReview } from '../hooks/useCardReview';

const MyComponent = () => {
  const {
    currentCard,
    showAnswer,
    loading,
    loadNextCard,
    reviewCard,
    revealAnswer,
  } = useCardReview(deckId);
  
  // Use the state and functions
  if (loading) return <LoadingSpinner />;
  // ...
};
```

**Example: useLocalStorage hook**
```typescript
import { useLocalStorage } from '../hooks/useLocalStorage';

const MyComponent = () => {
  const [value, setValue] = useLocalStorage('my-key', 'default');
  
  // Automatically syncs with localStorage
  setValue('new value');
};
```

### Using Constants:

**Error Messages:**
```typescript
import { ERROR_MESSAGES } from '../constants/messages';

setError(ERROR_MESSAGES.LOAD_DECKS_FAILED);
```

**Chart Colors:**
```typescript
import { CHART_COLORS, GRADE_COLOR_MAP } from '../constants/chartColors';

<Bar fill={CHART_COLORS.primary} />
<Cell fill={GRADE_COLOR_MAP['Good']} />
```

## 🧪 Testing the Features

### Quick Test Checklist:

- [ ] **Markdown in Feedback**
  - [ ] Enable AI mode
  - [ ] Submit an answer
  - [ ] Verify markdown renders (bold, lists, code blocks)

- [ ] **Enter Key Submission**
  - [ ] Type answer in textarea
  - [ ] Press Enter → submits
  - [ ] Press Shift+Enter → new line

- [ ] **Card Reveal (AI Disabled)**
  - [ ] Disable AI mode
  - [ ] Tap question card → reveals answer
  - [ ] Grade the card

- [ ] **Existing Features**
  - [ ] Upload .apkg file
  - [ ] Review cards
  - [ ] View statistics
  - [ ] Delete deck

## 🎯 Common Use Cases

### Use Case 1: Study with AI Assistance
```
1. Enable AI mode in settings
2. Configure your LLM (OpenAI, Gemini, etc.)
3. Review deck
4. Type answer → Press Enter to submit
5. Read AI feedback with formatting
6. Click "Reveal Answer Card"
7. Grade yourself
```

### Use Case 2: Traditional Flashcard Review
```
1. Disable AI mode in settings
2. Review deck
3. Read question
4. Think of answer
5. Tap card to reveal
6. Grade yourself
```

### Use Case 3: Advanced AI Feedback
```
If your LLM returns feedback like:

**Great job!** Your answer is correct.

Here's more detail:
- Point 1 about the answer
- Point 2 with clarification

```python
# Code example
def example():
    return "This will be syntax highlighted"
```

All of this will render beautifully with proper formatting!
```

## 🔧 Troubleshooting

### Issue: Markdown not rendering
**Solution:** 
- Ensure AI mode is enabled
- Check that LLM is configured correctly
- Verify API key is valid

### Issue: Enter key not working
**Solution:**
- Ensure you're in AI mode (answer textarea visible)
- Textarea must have focus
- Use Shift+Enter for new lines

### Issue: Card won't reveal (AI disabled)
**Solution:**
- Ensure AI mode is disabled in settings
- Click directly on the question card
- Cursor should change to pointer on hover

## 📱 Responsive Design

All features work across devices:
- ✅ Desktop (full experience)
- ✅ Tablet (optimized layout)
- ✅ Mobile (touch-friendly)

## 🎨 Styling

### Markdown Styling
All markdown elements are styled to match the app theme:
- Code blocks have dark background
- Lists are properly indented
- Headings use accent color
- Blockquotes have left border
- Tables are responsive

### Custom CSS Classes
```css
.feedback-content code { /* inline code */ }
.feedback-content pre { /* code blocks */ }
.feedback-content strong { /* bold text */ }
.feedback-content em { /* italic text */ }
.feedback-content ul, ol { /* lists */ }
.feedback-content blockquote { /* quotes */ }
```

## 🚀 Performance

- **No impact on load time**
- **Markdown rendering is fast**
- **Hooks optimize re-renders**
- **Constants reduce bundle size**

## 📚 Additional Resources

- `REFACTORING_SUMMARY.md` - Detailed refactoring documentation
- `CODE_EXAMPLES.md` - Before/after code comparisons
- `IMPLEMENTATION_COMPLETE.md` - Implementation summary

## 💡 Tips

1. **Use Shift+Enter** for multi-line answers
2. **AI feedback** can include code examples with syntax highlighting
3. **Toggle between AI and original questions** for comparison
4. **Card reveal** works with both click and tap
5. **All features** are keyboard accessible

---

Enjoy the improved flashcard experience! 🎉
