# AI-Agent-Document_Processing
This is a console application that demonstrates how to use local LLMs via Ollama to extract structured information from text documents.

## New Features

### Enhanced Document Support
- **Text Files** (.txt) - Full UTF-8 support
- **PDF Documents** (.pdf) - Text extraction from all pages
- **Word Documents** (.docx) - Complete document processing

### Advanced AI Operations
1. **Document Summarization** - Generate comprehensive summaries with confidence scores
2. **Document Comparison** - Compare multiple documents with similarity analysis  
3. **Interactive Q&A** - Ask questions about your documents in real-time
4. **Entity Extraction** - Extract people, organizations, locations, dates, and key terms

### Production Features
- **Dependency Injection** - Clean, testable architecture
- **Structured Logging** - Comprehensive logging with multiple levels
- **Error Handling** - Robust error handling with retry mechanisms
- **Configuration Management** - Environment-based configuration
- **Docker Support** - Complete containerization with Docker Compose
- **Progress Indicators** - Real-time processing feedback

## Prerequisites

### Local Development
- .NET 8.0 SDK or later
- Ollama installed and running locally
- At least one language model (recommended: `phi3:mini`, `tinyllama`, or `llama2`)

### Docker Deployment
- Docker Desktop or Docker Engine
- Docker Compose v2+
- At least 4GB available RAM

## Installation & Setup

### Option 1: Local Development

1. **Install dependencies**
```bash
dotnet restore
```

2. **Setup Ollama and models**
```bash
# Start Ollama service
ollama serve

# Pull required model (choose one)
ollama pull tinyllama      # Fastest, good for testing
ollama pull phi3:mini      # Balanced performance
ollama pull llama2         # Better quality, slower
```

3. **Prepare documents**
```bash
# Create sample documents directory
mkdir SampleTexts

# Add your documents (.txt, .pdf, .docx files)
```

4. **Run the application**
```bash
dotnet run
```

### Option 2: Docker Deployment

1. **Setup with Docker Compose**
```bash
# Start services
docker-compose up -d

# Pull AI model into Ollama container
docker exec -it ollama-server ollama pull tinyllama

# Run the document processor (interactive mode)
docker-compose exec document-processor dotnet AI-Agent-Document_Processing.dll
```

2. **Add documents to process**
```bash
# Copy documents to the SampleTexts directory
# The container will automatically detect them
```

## Project Structure

```
AI-Agent-Document_Processing/
├── Program.cs                    # Application entry point
├── Services/
│   ├── IOllamaService.cs        # Ollama API service interface
│   ├── OllamaService.cs         # Ollama API implementation
│   ├── IDocumentReader.cs       # Document reading interface  
│   └── DocumentReader.cs        # Multi-format document reader
├── Models/
│   ├── DocumentInfo.cs          # Document metadata model
│   ├── AppSettings.cs           # Configuration model
│   └── OllamaModels.cs          # API response models
├── SampleTexts/                 # Input documents directory
├── Analysis/                    # Output JSON files directory
├── Logs/                        # Application logs directory
├── appsettings.json            # Configuration file
├── Dockerfile                  # Container definition
├── docker-compose.yml          # Multi-container setup
└── DocumentProcessingAgent.csproj # Project file
```

## Configuration

### Environment Variables

```bash
# Ollama Configuration
OLLAMA_ENDPOINT=http://localhost:11434/api/chat
OLLAMA_MODEL=tinyllama

# Application Settings  
ASPNETCORE_ENVIRONMENT=Development
```

## Usage Examples

### 1. Document Summarization
```
Select an operation: 1
Enter the document number: 1
Processing: research_paper.pdf
Loaded document with 15,420 characters. Generating summary...

AI Response:
{
  "title": "Climate Change Adaptation Strategies",
  "summary": "Comprehensive analysis of adaptation strategies...",
  "keyPoints": [
    "Rising sea levels pose significant challenges",
    "Technology solutions show promise",
    "Policy coordination is essential"
  ],
  "confidence": 0.87
}
```

### 2. Compare Multiple Documents
```
Select an operation: 2
Select documents to compare:
Enter document numbers (comma-separated, e.g., 1,2,3):
Selection: 1,3
Loaded: research_paper_v1.pdf
Loaded: research_paper_v2.pdf

Processing comparison...

AI Response:
{
  "documents": [
    {
      "fileName": "research_paper_v1.pdf",
      "mainTopic": "Climate Change Mitigation Strategies",
      "documentType": "Research Paper"
    },
    {
      "fileName": "research_paper_v2.pdf", 
      "mainTopic": "Climate Change Mitigation and Adaptation Strategies",
      "documentType": "Research Paper"
    }
  ],
  "commonalities": [
    "Both discuss renewable energy solutions",
    "Similar methodology for carbon footprint analysis",
    "Shared references to IPCC reports",
    "Common focus on policy recommendations"
  ],
  "differences": [
    "Version 2 includes new section on adaptation strategies",
    "Updated statistics and data from 2024 in version 2",
    "Version 1 focuses primarily on mitigation, version 2 is more comprehensive",
    "Different case studies: v1 uses European examples, v2 includes global cases"
  ],
  "similarityScore": 0.73,
  "recommendation": "Version 2 represents a significant expansion with added adaptation content and updated data. Consider using version 2 for current research as it includes more recent findings and broader scope."
}
```

### 3. Interactive Document Q&A
```
Select an operation: 3
Select documents: 1,2

Document Q&A Session
====================
Question: What are the main financial risks mentioned?

AI Response:
{
  "answer": "The primary financial risks include market volatility, credit exposure, and regulatory compliance costs...",
  "confidence": 0.92,
  "sourceDocuments": ["financial_report.pdf"],
  "relevantQuotes": ["Market conditions remained challenging throughout Q3..."]
}
```

### 4. Entity Extraction
```
Select an operation: 4
Enter the document number: 2

AI Response:
{
  "people": ["Dr. Sarah Johnson", "Michael Chen", "Prof. Robert Smith"],
  "organizations": ["MIT", "Climate Research Institute", "UN IPCC"],
  "locations": ["Boston", "California", "Arctic Ocean"],
  "technologies": ["Machine Learning", "Remote Sensing", "IoT Sensors"]
}
```

## Architecture

### Key Services

#### OllamaService
- Handles all AI model communication
- Implements retry logic and error handling
- Manages request/response serialization

#### DocumentReader  
- Supports multiple document formats
- Handles file size and format validation
- Provides unified text extraction interface

#### DocumentProcessor
- Orchestrates document analysis workflows
- Manages structured AI prompts
- Handles result formatting and storage

## Performance Considerations

### AI Model Selection
- **tinyllama**: Fast processing, basic accuracy (~2GB RAM)
- **phi3:mini**: Balanced performance (~4GB RAM)  
- **llama2**: High accuracy, slower processing (~8GB RAM)


## Troubleshooting

### Common Issues

**Ollama Connection Failed**
```bash
# Check if Ollama is running
curl http://localhost:11434/api/tags

# Start Ollama service
ollama serve

# Verify model is installed
ollama list
```

**PDF Processing Errors**
```bash
# Install system dependencies (Linux)
sudo apt-get install libgdiplus libc6-dev

# For Windows, install Visual C++ Redistributables
```

**Memory Issues with Large Documents**
```bash
# Increase Docker container memory
docker-compose up -d --memory=8g
```

### Logging and Diagnostics

Logs are written to:
- **Console**: Real-time information and errors
- **File**: `Logs/app.log` with detailed diagnostic information
- **Docker**: Use `docker-compose logs document-processor`

## Security Considerations

- **No External Network Access**: All processing happens locally
- **File System Isolation**: Documents processed in sandboxed directories  
- **No Data Persistence**: No sensitive information stored permanently

## Deployment Options

### Development
```bash
dotnet run --environment Development
```

### Docker
```bash
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

### Cloud Deployment
The application can be deployed to:
- **Azure Container Instances**
- **AWS ECS/Fargate**  
- **Google Cloud Run**
- **Kubernetes clusters**

## Future Enhancements

- **Web API Interface** - REST API for remote document processing
- **Batch Processing** - Process multiple documents simultaneously
- **Custom Model Training** - Fine-tune models for specific domains
- **Advanced Analytics** - Document similarity clustering and visualization
- **Multi-language Support** - Process documents in various languages
- **Integration APIs** - Connect with document management systems