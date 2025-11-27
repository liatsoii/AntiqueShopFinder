import 'package:flutter/material.dart';
import 'package:hive_flutter/hive_flutter.dart';
import '../models/note.dart';
import '../services/note_storage.dart';
import '../widgets/note_card.dart';
import '../widgets/note_dialog.dart';

class HomeScreen extends StatefulWidget {
  const HomeScreen({Key? key}) : super(key: key);

  @override
  _HomeScreenState createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  late Box<Note> notesBox;

  @override
  void initState() {
    super.initState();
    notesBox = Hive.box<Note>(NoteStorage.boxName);
  }

  void _addOrEditNote({Note? note, int? index}) {
    showDialog(
      context: context,
      builder: (_) => NoteDialog(
        note: note,
        onSave: (title, content) {
          final newNote = Note(title: title, content: content);
          if (note == null) {
            NoteStorage.addNote(newNote);
          } else if (index != null) {
            NoteStorage.updateNote(index, newNote);
          }
        },
      ),
    );
  }

  void _deleteNote(int index) {
    NoteStorage.deleteNote(index);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFFFFF8E1), 
      appBar: AppBar(
        title: const Text('Мои заметки'),
        backgroundColor: Colors.yellow[700],
      ),
      body: ValueListenableBuilder(
        valueListenable: notesBox.listenable(),
        builder: (context, Box<Note> box, _) {
          if (box.values.isEmpty) {
            return const Center(child: Text('Нет заметок'));
          }
          return ListView.builder(
            itemCount: box.length,
            itemBuilder: (context, index) {
              final note = box.getAt(index);
              return NoteCard(
                note: note!,
                onEdit: () => _addOrEditNote(note: note, index: index),
                onDelete: () => _deleteNote(index),
              );
            },
          );
        },
      ),
      floatingActionButton: FloatingActionButton(
        onPressed: () => _addOrEditNote(),
        backgroundColor: Colors.yellow[700],
        child: const Icon(Icons.add),
      ),
    );
  }
}